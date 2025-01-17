﻿using Aidan.Common.Core;
using Aidan.Common.Core.Enum;
using BrunelUni.IntelliFarm.Core.Dtos;
using BrunelUni.IntelliFarm.Tests.Unit.Data.Constants;
using NSubstitute;
using NUnit.Framework;

namespace BrunelUni.IntelliFarm.Tests.Unit.Data.DataAccess.AnimationContextTests.Initialize
{
    public class When_Initialized_Successfully_If_Blender_Is_Not_Installed : Given_A_BlenderAnimationContext
    {
        private string _baseUrl;

        protected override void When( )
        {
            _baseUrl = "https://apiendpoint.com/";
            MockConfigurationAdapter.Get<MainAppOptions>( )
                .Returns( new MainAppOptions
                {
                    ApiBaseUrl = _baseUrl
                } );
            MockFileAdapter.Exists( Arg.Any<string>( ) )
                .Returns( Result.Error( "file doesnt exist" ) );
            MockWebClientAdapter.DownloadFile( Arg.Any<string>( ), Arg.Any<string>( ) )
                .Returns( new Result { Status = OperationResultEnum.Success } );
            MockZipAdapter.ExtractToDirectory( Arg.Any<string>( ), Arg.Any<string>( ) )
                .Returns( new Result { Status = OperationResultEnum.Success } );
            SUT.Initialize( );
        }

        [ Test ]
        public void Then_Correct_File_Was_Downloaded( )
        {
            MockWebClientAdapter.Received( ).DownloadFile(
                $"{_baseUrl}blender",
                "blender.zip" );
            MockWebClientAdapter.Received( 1 ).DownloadFile( Arg.Any<string>( ), Arg.Any<string>( ) );
        }

        [ Test ]
        public void Then_It_Checks_If_Blender_Is_Already_Downloaded( )
        {
            MockFileAdapter.Received( ).Exists( TestConstants.BlenderDirectory );
            MockFileAdapter.Received( 1 ).Exists( Arg.Any<string>( ) );
        }

        [ Test ]
        public void Then_File_Is_Unzipped( )
        {
            MockZipAdapter.Received( ).ExtractToDirectory( "blender.zip", $"{TestConstants.Directory}\\blender" );
            MockZipAdapter.Received( 1 ).ExtractToDirectory( Arg.Any<string>( ), Arg.Any<string>( ) );
        }

        [ Test ]
        public void Then_File_Is_Unzipped_After_Its_Downloaded_And_File_Is_Checked_First( )
        {
            Received.InOrder( ( ) =>
            {
                MockFileAdapter.Exists( Arg.Any<string>( ) );
                MockWebClientAdapter.DownloadFile( Arg.Any<string>( ), Arg.Any<string>( ) );
                MockZipAdapter.ExtractToDirectory( Arg.Any<string>( ), Arg.Any<string>( ) );
            } );
        }

        [ Test ]
        public void Then_Python_Source_Files_Are_Bundled( )
        {
            MockPythonBundler.Received( 1 ).Bundle( Arg.Any<string>( ), Arg.Any<string>( ) );
            MockPythonBundler.Received( ).Bundle( TestConstants.BlenderScriptsModulesDirectory,
                TestConstants.DataScriptsDir );
        }
    }
}