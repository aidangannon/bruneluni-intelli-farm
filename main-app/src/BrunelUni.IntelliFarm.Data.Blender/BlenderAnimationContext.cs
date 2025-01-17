﻿using System;
using System.IO;
using System.Net;
using Aidan.Common.Core.Enum;
using Aidan.Common.Core.Interfaces.Contract;
using BrunelUni.IntelliFarm.Core.Dtos;
using BrunelUni.IntelliFarm.Data.Core.Dtos;
using BrunelUni.IntelliFarm.Data.Core.Interfaces.Contract;

namespace BrunelUni.IntelliFarm.Data.Blender
{
    public class BlenderAnimationContext : IAnimationContext
    {
        private readonly IFileAdapter _fileAdapter;
        private readonly IPythonBundler _pythonBundler;
        private readonly IConfigurationAdapter _configurationAdapter;
        private readonly ILoggerAdapter<IAnimationContext> _loggerAdapter;
        private readonly IRenderManagerFactory _renderManagerFactory;
        private readonly IRenderManagerService _renderManagerService;
        private readonly IScriptsRootDirectoryState _scriptsRootDirectoryState;
        private readonly IWebClientAdapter _webClientAdapter;
        private readonly IZipAdapter _zipAdapter;

        public BlenderAnimationContext( IRenderManagerService renderManagerService,
            IRenderManagerFactory renderManagerFactory,
            IFileAdapter fileAdapter,
            IZipAdapter zipAdapter,
            IScriptsRootDirectoryState scriptsRootDirectoryState,
            IWebClientAdapter webClientAdapter,
            IPythonBundler pythonBundler,
            IConfigurationAdapter configurationAdapter,
            ILoggerAdapter<IAnimationContext> loggerAdapter )
        {
            _renderManagerService = renderManagerService;
            _renderManagerFactory = renderManagerFactory;
            _fileAdapter = fileAdapter;
            _zipAdapter = zipAdapter;
            _scriptsRootDirectoryState = scriptsRootDirectoryState;
            _webClientAdapter = webClientAdapter;
            _pythonBundler = pythonBundler;
            _configurationAdapter = configurationAdapter;
            _loggerAdapter = loggerAdapter;
        }

        public void Initialize( )
        {
            if( _fileAdapter.Exists( _scriptsRootDirectoryState.ScriptsRootDirectoryDto.BlenderDirectory ).Status ==
                OperationResultEnum.Success )
            {
                _loggerAdapter.LogInfo( "blender already installed" );
                _loggerAdapter.LogInfo( "bundling python scripts" );
                _pythonBundler.Bundle(
                    _scriptsRootDirectoryState.ScriptsRootDirectoryDto.BlenderScriptsModulesDirectory,
                    _scriptsRootDirectoryState.ScriptsRootDirectoryDto.DataScriptsDir );
                return;
            }

            _loggerAdapter.LogInfo( "blender installing for the first time" );
            var webResult = _webClientAdapter.DownloadFile(
                $"{_configurationAdapter.Get<MainAppOptions>( ).ApiBaseUrl}blender",
                "blender.zip" );
            _loggerAdapter.LogInfo( "file downloaded" );
            if( webResult.Status == OperationResultEnum.Failed )
                throw new WebException( $"failing to download file msg: {webResult.Msg}" );
            var zipResult =
                _zipAdapter.ExtractToDirectory( "blender.zip",
                    $"{_scriptsRootDirectoryState.ScriptsRootDirectoryDto.Directory}\\blender" );
            _loggerAdapter.LogInfo( $"file unzipped to {_scriptsRootDirectoryState.ScriptsRootDirectoryDto.Directory}\\blender" );
            if( zipResult.Status == OperationResultEnum.Failed )
                throw new IOException( $"failed to zip file {zipResult.Msg}" );
            _loggerAdapter.LogInfo( "bundling python scripts" );
            _pythonBundler.Bundle( _scriptsRootDirectoryState.ScriptsRootDirectoryDto.BlenderScriptsModulesDirectory,
                _scriptsRootDirectoryState.ScriptsRootDirectoryDto.DataScriptsDir );
            _loggerAdapter.LogInfo( "blender initialized" );
        }

        public void InitializeScene( string filePath )
        {
            if( _fileAdapter.Exists( filePath ).Status == OperationResultEnum.Failed )
            {
                throw new ArgumentException( $"{filePath} was not found" );
            }

            var ext = _fileAdapter.GetFileExtension( filePath ).Value;
            if( ext != ".blend" ) { throw new ArgumentException( $"{filePath} is not of type '.blend'" ); }

            _loggerAdapter.LogInfo( $"initialize scene at {filePath}" );
            _renderManagerService.RenderManager = _renderManagerFactory.Factory( new RenderMetaDto
            {
                BlendFilePath = filePath
            } );
            _loggerAdapter.LogInfo( $"initialized scene at {filePath}" );
        }
    }
}