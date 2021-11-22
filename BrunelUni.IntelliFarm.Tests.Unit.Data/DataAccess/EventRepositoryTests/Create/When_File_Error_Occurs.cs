﻿using Aidan.Common.Core;
using Aidan.Common.Core.Enum;
using BrunelUni.IntelliFarm.Data.Core.Dtos;
using NSubstitute;
using NUnit.Framework;

namespace BrunelUni.IntelliFarm.Tests.Unit.Data.DataAccess.EventRepositoryTests.Create
{
    public class When_File_Error_Occurs : Given_An_EventRepository
    {
        private ObjectResult<RenderResultDto> _result;
        private const string Message = "message";
        
        protected override void When( )
        {
            MockSceneProcessor.ReadTemp<RenderResultDto>( )
                .Returns( new ObjectResult<RenderResultDto>
                {
                    Status = OperationResultEnum.Failed,
                    Msg = Message
                } );
            MockRenderManagerService
                .RenderManager
                .GetRenderInfo( )
                .Returns( new RenderMetaDto( ) );
            MockSceneProcessor.RunSceneProcessAndExit( Arg.Any<string>( ), Arg.Any<string>( ), Arg.Any<bool>( ) )
                .Returns( Result.Success( ) );
            _result = SUT.Create( );
        }
        
        [ Test ]
        public void Then_Failiure_Is_Returned( )
        {
            Assert.AreEqual( OperationResultEnum.Failed, _result.Status );
            Assert.AreEqual( Message, _result.Msg );
        }
    }
}