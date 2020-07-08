# Event System
## What is the Event System?
A high performance solution for safely creating events between systems in Unity ECS.

## Features
- High performance, negligible overhead
- Same frame producing then consuming
- Share events across worlds
- Any system order
- Support different update rates. e.g. produce in fixed update, consume in update
- Any number of producers and consumers
- Fully threaded
- No sync points
- No garbage
- Easy dependency management
- Safety checks
- Support streams of data

## Writing
### Nondetermistic Mode
Provides convenience and performance at the cost of being nondeterministic 

**Entities.ForEach**
```csharp
var writer = this.eventSystem.CreateEventWriter<YourEvent>();

this.Entities.ForEach((Entity entity) =>
	{
		writer.Write(new YourEvent());
	})
	.ScheduleParallel();

this.eventSystem.AddJobHandleForProducer<YourEvent>(this.Dependency);
```

**IJobChunk**
// TODO

### Deterministic Mode
Provides determinism by manually handling indexing.
At high entity count this is quite a bit slower using Entities.ForEach and it is recommended you use IJobChunk for performance.

**Entities.ForEach**
```csharp
var writer = this.eventSystem.CreateEventWriter<YourEvent>(this.query.CalculateEntityCount());

this.Entities.ForEach((Entity entity, int entityInQueryIndex) =>
	{
		writer.BeginForEachIndex(entityInQueryIndex);
		writer.Write(new YourEvent());
	})
	.WithStoreEntityQueryInField(ref this.query)
	.ScheduleParallel();

this.eventSystem.AddJobHandleForProducer<YourEvent>(this.Dependency);
```

**IJobChunk**
// TODO

## Reading
Reader is the same regardless of writing mode

### IJobEvent
IJobEvent is the easiest way to read back events when no extra data is being streamed.
To create a job just implement the IJobEvent<T> interface.

```csharp
[BurstCompile]
private struct EventJob : IJobEvent<YourEvent>
{
	public NativeQueue<int>.ParallelWriter Counter;

	public void Execute(YourEvent e)
	{
		// do what you want
	}
}
```

```csharp
var eventSystem = this.World.GetOrCreateSystem<EventSystem>();

this.Dependency = new EventJob
{
	// assign job fields
}
.Schedule<EventJob, YourEvent>(eventSystem);
```

Each reader will be schedule to be read one after the other. The reading process itself is done in parallel.
If you want each reader to be read in parallel to each other, you can use ScheduleSimultaneous instead.
`.ScheduleSimultaneous<EventJob, YourEvent>(eventSystem));`

### IJobEventStream
Sometimes you need a bit more control over reading as the event system allows streaming of any type of data in your events.
IJobEventStream gives you direct access to the reader allowing you to read it back in whatever format you desire.
```csharp
[BurstCompile]
private struct EventStreamJob : IJobEventStream<YourEvent>
{
	public void Execute(NativeEventStream.Reader stream, int index)
	{
		var count = stream.BeginForEachIndex(index);
		
		for (var i = 0; i < count; i += 4)
		{
			var e = stream.Read<YourEvent>();
			var d1 = stream.Read<YourData1>();
			var d2 = stream.Read<YourData2>();
			var d3 = stream.Read<YourData3>();
		}
	}
}
```

```csharp
var eventSystem = this.World.GetOrCreateSystem<EventSystem>();

this.Dependency = new EventStreamJob
{
	// assign job fields
}
.Schedule<EventStreamJob, YourEvent>(eventSystem);
```

It is scheduled the same way and has the same ScheduleSimultaneous option.
`.ScheduleSimultaneous<EventStreamJob, YourEvent>(eventSystem));`

### ConsumeSingleEventSystemBase
If you need to read your events on the main thread you can use ConsumeSingleEventSystemBase.
```csharp
public class MyEventSystem : ConsumeSingleEventSystemBase<MyEvent>
{
	protected override void OnEvent(MyEvent e)
	{

	}
}
```

### ConsumeEventSystemBase
Again if you need a bit more control over reading your events, you can implement ConsumeEventSystemBase instead.
```csharp
public class MyEventSystem : ConsumeEventSystemBase<MyEvent>
{
	protected override void OnEventStream(ref NativeEventStream.Reader reader, int eventCount)
	{
		
	}
}
```

### Manual iteration
// TODO

```csharp
this.Dependency = this.eventSystem.GetEventReaders<T>(this.Dependency, out IReadOnlyList<NativeEventStream.Reader> readers);

this.eventSystem.AddJobHandleForConsumer<T>(this.Dependency);
```

### Extensions
// TODO

### Benchmarks
// TODO