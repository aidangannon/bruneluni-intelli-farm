﻿using Aidan.Common.Core.Attributes;
using Aidan.Common.Core.Enum;

namespace BrunelUni.IntelliFarm.Data.Core.Interfaces.Contract
{
    [ Service( Scope = ServiceLifetimeEnum.Scoped ) ]
    public interface IRenderManagerService
    {
        IRenderManager RenderManager { get; set; }
    }
}