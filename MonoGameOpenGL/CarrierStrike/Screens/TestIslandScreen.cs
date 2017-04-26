﻿using MonoGameEngineCore;
using System;
using Microsoft.Xna.Framework;
using SystemWar;
using MonoGameEngineCore.Rendering.Camera;
using Microsoft.Xna.Framework.Input;
using MonoGameEngineCore.Rendering;
using MonoGameEngineCore.Procedural;
using MonoGameEngineCore.GameObject;
using MonoGameEngineCore.Helper;
using CarrierStrike.Gameplay;
using MonoGameEngineCore.GameObject.Components;
using BEPUphysics.UpdateableSystems;
using System.Collections.Generic;

namespace CarrierStrike.Screens
{
    class TestIslandScreen : Screen
    {
        protected MouseFreeCamera mouseCamera;
        bool releaseMouse;

        public TestIslandScreen() : base()
        {

        }

        public override void OnInitialise()
        {
            SystemCore.CursorVisible = false;

            SystemCore.ActiveScene.SetUpBasicAmbientAndKey();
            SystemCore.ActiveScene.SetDiffuseLightDir(0, new Vector3(1, 1, 1));
            SystemCore.ActiveScene.FogEnabled = true;

            mouseCamera = new MouseFreeCamera(new Vector3(0, 0, 0));
            SystemCore.SetActiveCamera(mouseCamera);
            mouseCamera.moveSpeed = 0.1f;
            mouseCamera.SetPositionAndLook(new Vector3(50, 30, -20), (float)Math.PI, (float)-Math.PI / 5);

            AddInputBindings();

            SetUpSkyDome();

            SetUpGameWorld(100, 2, 2);

            Chopper chopper = new Chopper();
            chopper.Transform.SetPosition(new Vector3(60, 10, 60));
            SystemCore.GameObjectManager.AddAndInitialiseGameObject(chopper);

            Carrier carrier = new Carrier();
            carrier.Transform.SetPosition(new Vector3(50, 10, 50));
            SystemCore.GameObjectManager.AddAndInitialiseGameObject(carrier);
            

            carrier.GetComponent<PhysicsComponent>().PhysicsEntity.IsAffectedByGravity = true;
            carrier.GetComponent<PhysicsComponent>().PhysicsEntity.Mass = 100;
           

            SystemCore.PhysicsSimulation.ForceUpdater.Gravity = new BEPUutilities.Vector3(0, -0.81f, 0);

            var tris = new List<BEPUutilities.Vector3[]>();
            float basinWidth = 200;
            float basinLength = 200;
            float waterHeight = 5;

            //Remember, the triangles composing the surface need to be coplanar with the surface.  In this case, this means they have the same height.
            tris.Add(new[]
                         {
                             new BEPUutilities.Vector3(-basinWidth / 2, 0, -basinLength / 2), new BEPUutilities.Vector3(basinWidth / 2, 0, -basinLength / 2),
                             new BEPUutilities.Vector3(-basinWidth / 2, waterHeight, basinLength / 2)
                         });
            tris.Add(new[]
                         {
                             new BEPUutilities.Vector3(-basinWidth / 2, 0, basinLength / 2), new BEPUutilities.Vector3(basinWidth / 2, 0, -basinLength / 2),
                             new BEPUutilities.Vector3(basinWidth / 2, 0, basinLength / 2)
                         });
            var fluid = new FluidVolume(BEPUutilities.Vector3.Up, -0.81f, tris, 10, 0.8f, 0.8f, 0.8f);

            SystemCore.PhysicsSimulation.Add(fluid);

            base.OnInitialise();
        }


        private void AddInputBindings()
        {
            input = SystemCore.GetSubsystem<InputManager>();
            input.AddKeyDownBinding("CameraForward", Keys.Up);
            input.AddKeyDownBinding("CameraBackward", Keys.Down);
            input.AddKeyDownBinding("CameraLeft", Keys.Left);
            input.AddKeyDownBinding("CameraRight", Keys.Right);

            input.AddKeyPressBinding("MainMenu", Keys.Escape);

            var releaseMouseBinding = input.AddKeyPressBinding("MouseRelease", Keys.M);
            releaseMouseBinding.InputEventActivated += (x, y) =>
            {
                releaseMouse = !releaseMouse;
                SystemCore.CursorVisible = releaseMouse;
            };

            var binding = input.AddKeyPressBinding("WireframeToggle", Keys.Space);
            binding.InputEventActivated += (x, y) => { SystemCore.Wireframe = !SystemCore.Wireframe; };
        }


        private void SetUpGameWorld(int patchSize, int widthInTerrainPatches, int heightInTerrainPatches)
        {
            var noiseModule = NoiseGenerator.Island((patchSize * widthInTerrainPatches)/2, (patchSize * widthInTerrainPatches) / 2, 25, 0.08f, RandomHelper.GetRandomInt(1000));


            for (int i = 0; i < widthInTerrainPatches; i++)
                for (int j = 0; j < heightInTerrainPatches; j++)
                {
                    int xsampleOffset = i * (patchSize -1);
                    int zsampleOffset = j * (patchSize-1);

                    var hm = NoiseGenerator.CreateHeightMap(noiseModule, patchSize, 1, 40,  xsampleOffset,zsampleOffset, 1);
                    var hmObj = hm.CreateTranslatedRenderableHeightMap(Color.MonoGameOrange, EffectLoader.LoadSM5Effect("flatshaded"), new Vector3(xsampleOffset - 1, 0, zsampleOffset - 1));
                    SystemCore.GameObjectManager.AddAndInitialiseGameObject(hmObj);
                }


      

            Heightmap seaHeightMap = new Heightmap(patchSize/4 * widthInTerrainPatches, 1);
            var seaObject = seaHeightMap.CreateRenderableHeightMap(Color.Blue, EffectLoader.LoadSM5Effect("flatshaded"));
            seaObject.Transform.SetPosition(new Vector3(-50, 0, -50));
            seaObject.Transform.Scale = 10;
            SystemCore.GameObjectManager.AddAndInitialiseGameObject(seaObject);
        }

        private void SetUpSkyDome()
        {
            var skyDome = new GradientSkyDome(Color.MediumBlue, Color.LightCyan);

        }

        public override void OnRemove()
        {
            SystemCore.GUIManager.ClearAllControls();
            SystemCore.GameObjectManager.ClearAllObjects();
            SystemCore.ActiveScene.ClearLights();
            input.ClearBindings();
            base.OnRemove();
        }

        public override void Update(GameTime gameTime)
        {
            if (!SystemCore.CursorVisible)
            {

                mouseCamera.Slow = input.EvaluateInputBinding("SlowCamera");

                if (input.EvaluateInputBinding("CameraForward"))
                    mouseCamera.MoveForward();
                if (input.EvaluateInputBinding("CameraBackward"))
                    mouseCamera.MoveBackward();
                if (input.EvaluateInputBinding("CameraLeft"))
                    mouseCamera.MoveLeft();
                if (input.EvaluateInputBinding("CameraRight"))
                    mouseCamera.MoveRight();

                if (!releaseMouse)
                {
                    mouseCamera.Update(gameTime, input.MouseDelta.X, input.MouseDelta.Y);
                    input.CenterMouse();
                }
            }

            if (input.EvaluateInputBinding("MainMenu"))
                SystemCore.ScreenManager.AddAndSetActive(new MainMenuScreen());
            base.Update(gameTime);
        }

        public override void Render(GameTime gameTime)
        {

            base.Render(gameTime);
        }


    }
}
