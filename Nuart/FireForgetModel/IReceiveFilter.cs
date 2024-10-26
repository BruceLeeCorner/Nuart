﻿using System;

namespace Nuart.FireForgetModel
{
    public interface IReceiveFilter
    {
        void FilterCompletedFrames(byte[] dataReceived, out int[] frameEndingIndexes, bool hasBytesToRead);
    }
}