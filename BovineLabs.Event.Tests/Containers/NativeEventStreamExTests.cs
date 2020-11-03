// <copyright file="NativeEventStreamExTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if BL_TESTING

namespace BovineLabs.Event.Tests.Containers
{
    using BovineLabs.Event.Containers;
    using NUnit.Framework;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;

    /// <summary> Tests for <see cref="NativeEventStreamEx"/> . </summary>
    public class NativeEventStreamExTests
    {
        /// <summary> Tests the extensions AllocateLarge and ReadLarge. </summary>
        /// <param name="size"> The size of the allocation. </param>
        [TestCase(512)] // less than max size
        [TestCase(4092)] // max size
        [TestCase(8192)] // requires just more than 2 blocks
        public unsafe void WriteRead(int size)
        {
            var stream = new NativeEventStream(Allocator.Temp);

            var sourceData = new NativeArray<byte>(size, Allocator.Temp);
            for (var i = 0; i < size; i++)
            {
                sourceData[i] = (byte)(i % 255);
            }

            var writer = stream.AsThreadWriter();
            writer.Write(size);
            writer.AllocateLarge((byte*)sourceData.GetUnsafeReadOnlyPtr(), size);

            var reader = stream.AsReader();

            reader.BeginForEachIndex(0);

            var readSize = reader.Read<int>();

            Assert.AreEqual(size, readSize);

            var ptr = reader.ReadLarge(readSize);
            var result = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<byte>(ptr, readSize, Allocator.None);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref result, AtomicSafetyHandle.GetTempMemoryHandle());
#endif

            reader.EndForEachIndex();

            for (var i = 0; i < readSize; i++)
            {
                Assert.AreEqual(sourceData[i], result[i]);
            }
        }
    }
}

#endif