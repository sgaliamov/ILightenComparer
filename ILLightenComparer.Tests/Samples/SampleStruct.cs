﻿namespace ILLightenComparer.Tests.Samples
{
    public struct SampleStruct<TMember>
    {
        public TMember Field;

        public TMember Property { get; set; }
    }
}