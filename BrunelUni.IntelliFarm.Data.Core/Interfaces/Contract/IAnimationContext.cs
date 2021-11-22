﻿using Aidan.Common.Core;

namespace BrunelUni.IntelliFarm.Data.Core.Interfaces.Contract
{
    public interface IAnimationContext
    {
        Result Initialize( );
        Result InitializeScene( string filePath );
    }
}