﻿// <copyright file="IJobEventStream.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Event.Jobs
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using BovineLabs.Event.Containers;
    using BovineLabs.Event.Systems;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Jobs;
    using Unity.Jobs.LowLevel.Unsafe;

    /// <summary> Job that visits each event stream. </summary>
    /// <typeparam name="T"> Type of event. </typeparam>
    [JobProducerType(typeof(JobEventStream.EventJobStreamStruct<,>))]
    [SuppressMessage("ReSharper", "TypeParameterCanBeVariant", Justification = "Strict requirements for compiler")]
    [SuppressMessage("ReSharper", "UnusedTypeParameter", Justification = "Required by scheduler")]
    public interface IJobEventStream<T>
        where T : unmanaged
    {
        /// <summary> Executes the next event. </summary>
        /// <param name="stream"> The stream. </param>
        /// <param name="index"> The stream index. </param>
        void Execute(NativeThreadStream.Reader stream, int index);
    }

    /// <summary> Extension methods for <see cref="IJobEventStream{T}"/> . </summary>
    public static class JobEventStream
    {
        /// <summary> Schedule a <see cref="IJobEventStream{T}"/> job. </summary>
        /// <param name="jobData"> The job. </param>
        /// <param name="eventSystem"> The event system. </param>
        /// <param name="dependsOn"> The job handle dependency. </param>
        /// <typeparam name="TJob"> The type of the job. </typeparam>
        /// <typeparam name="T"> The type of the key in the hash map. </typeparam>
        /// <returns> The handle to job. </returns>
        public static unsafe JobHandle Schedule<TJob, T>(
            this TJob jobData, EventSystem eventSystem, JobHandle dependsOn = default)
            where TJob : struct, IJobEventStream<T>
            where T : unmanaged
        {
            dependsOn = eventSystem.GetEventReaders<T>(dependsOn, out var events);

            for (var i = 0; i < events.Count; i++)
            {
                var fullData = new EventJobStreamStruct<TJob, T>
                {
                    Readers = events[i],
                    JobData = jobData,
                    Index = i,
                };

                var scheduleParams = new JobsUtility.JobScheduleParameters(
                    UnsafeUtility.AddressOf(ref fullData),
                    EventJobStreamStruct<TJob, T>.Initialize(),
                    dependsOn,
                    ScheduleMode.Batched);

                dependsOn = JobsUtility.Schedule(ref scheduleParams);
            }

            eventSystem.AddJobHandleForConsumer<T>(dependsOn);

            return dependsOn;
        }

        /// <summary> Schedule a <see cref="IJobEventStream{T}"/> job. </summary>
        /// <param name="jobData"> The job. </param>
        /// <param name="eventSystem"> The event system. </param>
        /// <param name="dependsOn"> The job handle dependency. </param>
        /// <typeparam name="TJob"> The type of the job. </typeparam>
        /// <typeparam name="T"> The type of the key in the hash map. </typeparam>
        /// <returns> The handle to job. </returns>
        public static unsafe JobHandle ScheduleParallel<TJob, T>(
            this TJob jobData, EventSystem eventSystem, JobHandle dependsOn = default)
            where TJob : struct, IJobEventStream<T>
            where T : unmanaged
        {
            dependsOn = eventSystem.GetEventReaders<T>(dependsOn, out var events);

            var input = dependsOn;

            for (var i = 0; i < events.Count; i++)
            {
                var fullData = new EventJobStreamStruct<TJob, T>
                               {
                                   Readers = events[i],
                                   JobData = jobData,
                                   Index = i,
                               };

                var scheduleParams = new JobsUtility.JobScheduleParameters(
                                                                           UnsafeUtility.AddressOf(ref fullData),
                                                                           EventJobStreamStruct<TJob, T>.Initialize(),
                                                                           input,
                                                                           ScheduleMode.Batched);

                var handle = JobsUtility.Schedule(ref scheduleParams);
                dependsOn = JobHandle.CombineDependencies(dependsOn, handle);
            }

            eventSystem.AddJobHandleForConsumer<T>(dependsOn);

            return dependsOn;
        }

        /// <summary> The job execution struct. </summary>
        /// <typeparam name="TJob"> The type of the job. </typeparam>
        /// <typeparam name="T"> The type of the event. </typeparam>
        internal struct EventJobStreamStruct<TJob, T>
            where TJob : struct, IJobEventStream<T>
            where T : unmanaged
        {
            /// <summary> The <see cref="NativeThreadStream.Reader"/> . </summary>
            [ReadOnly]
            public NativeThreadStream.Reader Readers;

            /// <summary> The job. </summary>
            public TJob JobData;

            /// <summary> The index of the reader. </summary>
            public int Index;

            // ReSharper disable once StaticMemberInGenericType
            private static IntPtr jobReflectionData;

            private delegate void ExecuteJobFunction(
                ref EventJobStreamStruct<TJob, T> fullData,
                IntPtr additionalPtr,
                IntPtr bufferRangePatchData,
                ref JobRanges ranges,
                int jobIndex);

            /// <summary> Initializes the job. </summary>
            /// <returns> The job pointer. </returns>
            public static IntPtr Initialize()
            {
                if (jobReflectionData == IntPtr.Zero)
                {
                    jobReflectionData = JobsUtility.CreateJobReflectionData(
                        typeof(EventJobStreamStruct<TJob, T>),
                        typeof(TJob),
                        JobType.Single,
                        (ExecuteJobFunction)Execute);
                }

                return jobReflectionData;
            }

            /// <summary> Executes the job. </summary>
            /// <param name="fullData"> The job data. </param>
            /// <param name="additionalPtr"> AdditionalPtr. </param>
            /// <param name="bufferRangePatchData"> BufferRangePatchData. </param>
            /// <param name="ranges"> The job range. </param>
            /// <param name="jobIndex"> The job index. </param>
            [SuppressMessage("ReSharper", "MemberCanBePrivate.Global", Justification = "Required by burst.")]
            public static void Execute(
                ref EventJobStreamStruct<TJob, T> fullData,
                IntPtr additionalPtr,
                IntPtr bufferRangePatchData,
                ref JobRanges ranges,
                int jobIndex)
            {
                fullData.JobData.Execute(fullData.Readers, fullData.Index);
            }
        }
    }
}