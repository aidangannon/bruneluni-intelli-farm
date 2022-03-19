﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using Aidan.Common.Core;
using Aidan.Common.Core.Interfaces.Contract;
using BrunelUni.IntelliFarm.Core.Dtos;
using BrunelUni.IntelliFarm.Core.Enums;
using BrunelUni.IntelliFarm.Core.Interfaces.Contract;
using BrunelUni.IntelliFarm.Data.Core.Dtos;
using BrunelUni.IntelliFarm.Data.Core.Interfaces.Contract;

namespace BrunelUni.IntelliFarm.Domain
{
    public class IntelliFarmFacade : IIntelliFarmFacade
    {
        private readonly IWebClient _webClient;
        private readonly IConfigurationAdapter _configurationAdapter;
        private readonly IZipAdapter _zipAdapter;
        private readonly ISceneCommandFacade _sceneCommandFacade;
        private readonly IAnimationContext _animationContext;
        private readonly IFileAdapter _fileAdapter;
        private readonly IState _state;
        private readonly IRemoteFileService _remoteFileService;
        private readonly IRenderAnalyser _renderAnalyser;

        public IntelliFarmFacade( IWebClient webClient,
            IConfigurationAdapter configurationAdapter,
            IZipAdapter zipAdapter,
            ISceneCommandFacade sceneCommandFacade,
            IAnimationContext animationContext,
            IFileAdapter fileAdapter,
            IState state,
            IRemoteFileService remoteFileService,
            IRenderAnalyser renderAnalyser )
        {
            _webClient = webClient;
            _configurationAdapter = configurationAdapter;
            _zipAdapter = zipAdapter;
            _sceneCommandFacade = sceneCommandFacade;
            _animationContext = animationContext;
            _fileAdapter = fileAdapter;
            _state = state;
            _remoteFileService = remoteFileService;
            _renderAnalyser = renderAnalyser;
        }

        public Result CreateProject( string name, string filePath, params string [ ] devices )
        {
            var fileName = Path.GetFileNameWithoutExtension( filePath );
            if( fileName != name )
            {
                return Result.Error(
                    $"{fileName} is not the same as {name}, file name and scene name have to be the same" );
            }
            _zipAdapter.ExtractToDirectory( filePath, $"{_fileAdapter.GetCurrentDirectory( ).Value}\\{name}.zip" );
            var key = _webClient.UploadFile( "upload-file", $"{_fileAdapter.GetCurrentDirectory( ).Value}\\{name}.zip" );
            _animationContext.Initialize(  );
            _animationContext.InitializeScene( filePath );
            var sceneData = _sceneCommandFacade.GetSceneData( );
            var sceneDto = new SceneDto( );
            var frameLength = sceneData.EndFrame - sceneData.StartFrame + 1;
            var frames = new List<FrameDto>( );
            for( var i = 0; i < frameLength; i++ )
            {
                frames.Add( new FrameDto
                {
                    Scene = sceneDto.Id,
                    Number = sceneData.StartFrame + i
                } );
            }

            sceneDto.StartFrame = sceneData.StartFrame;
            sceneDto.FileName = key;
            sceneDto.Frames = frames.ToArray( );
            sceneDto.Status = RenderStatusEnum.NotStarted;
            sceneDto.Clients = devices.Select( x => new ClientDto
            {
                Name = x
            } ).ToArray( );
            var result = _webClient.Create( "scene", sceneDto );
            if( result.StatusCode == HttpStatusCode.NotFound )
            {
                return Result.Error( "cannot be created because devices cannot be found" );
            }

            return Result.Success( );
        }

        private double CalibrateScene( string name )
        {
            _animationContext.InitializeScene( $"{_fileAdapter.GetCurrentDirectory( ).Value}\\{name}" );
            _sceneCommandFacade.SetSceneData( new RenderDataDto
            {
                DiffuseBounces = 0,
                EndFrame = 1,
                StartFrame = 1,
                MaxBounces = 0,
                Samples = 100
            } );
            var times = new List<double>( );
            for( var i = 0; i < 3; i++ )
            {
                times.Add( _sceneCommandFacade.Render( ).RenderTime );
            }

            return times.Sum( ) / 3;
        }

        public void CreateDevice( string deviceName )
        {
            var file = _webClient.DownloadFile( "calibration-files", "calibration.zip" );
            _zipAdapter.ExtractToDirectory( file, _fileAdapter.GetCurrentDirectory( ).Value );
            _animationContext.Initialize(  );
            var avTimeForBaseScene = CalibrateScene( "poly_80_100_coverage.blend" );
            var avTimeFor0Scene = CalibrateScene( "vewiport_0.blend" );
            _webClient.Create( "device", new ClientDto
            {
                Name = deviceName,
                TimeFor80Poly100Coverage0Bounces100Samples = avTimeForBaseScene,
                TimeFor0PolyViewpoint = avTimeFor0Scene
            } );
        }

        public Result Render( string sceneName, string deviceName )
        {
            var webResult = _webClient.Get( $"bucket?sceneName={sceneName}&device={deviceName}" );
            if( webResult.StatusCode == HttpStatusCode.NotFound )
            {
                return Result.Error( $"bucket of scene: {sceneName} and device: {deviceName} not found" );
            }
            var bucket = webResult.Data as BucketDto;
            _webClient.DownloadFile( $"scene-file?key={bucket?.FilePath}", $"{sceneName}.blend" );
            _zipAdapter.ExtractToDirectory( $"{_fileAdapter.GetCurrentDirectory( ).Value}\\{sceneName}.zip",
                $"{_fileAdapter.GetCurrentDirectory( ).Value}" );
            _animationContext.Initialize( );
            _animationContext.InitializeScene( $"{_fileAdapter.GetCurrentDirectory( ).Value}\\{sceneName}.blend" );
            var times = new List<FrameTimeDto>( );
            foreach( var frame in bucket.Frames )
            {
                _sceneCommandFacade.SetSceneData( new RenderDataDto
                {
                    StartFrame = frame.Num,
                    EndFrame = frame.Num
                } );
                times.Add( new FrameTimeDto
                {
                    Num = frame.Num,
                    Time = _sceneCommandFacade.Render( ).RenderTime
                } );
            }

            _webClient.Create( "bucket", new BucketDto
            {
                DeviceId = bucket.DeviceId,
                SceneId = bucket.SceneId,
                Type = BucketTypeEnum.Actual,
                Frames = times
            } );
            return Result.Success( );
        }

        public void CreateBucketsFromProject( SceneDto sceneDto )
        {
            
        }
    }
}