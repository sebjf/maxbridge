﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace AsyncPipes.SharedMemory
{
    /*
    unsafe public class SharedMemory
    {
        protected MemoryMappedFile memoryMappedFile;
        protected MemoryMappedViewAccessor memoryMappedAccessor;
        protected string name;
        protected byte* ptr;
        public int size;

        public SharedMemory()
        {
            ptr = (byte*)0;
        }

        public byte* Open(string name)
        {
            if (this.name == name)
            {
                return ptr;
            }

            if (memoryMappedAccessor != null)
            {
                memoryMappedAccessor.SafeMemoryMappedViewHandle.ReleasePointer();
                memoryMappedAccessor = null;
            }

            memoryMappedFile = MemoryMappedFile.OpenExisting(name);
            memoryMappedAccessor = memoryMappedFile.CreateViewAccessor();
            memoryMappedAccessor.SafeMemoryMappedViewHandle.AcquirePointer(ref ptr);

            size = (int)memoryMappedAccessor.SafeMemoryMappedViewHandle.ByteLength;

            return ptr;
        }
    }
     * */
}
