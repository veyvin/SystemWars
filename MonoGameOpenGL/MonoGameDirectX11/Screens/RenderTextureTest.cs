﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameEngineCore.Camera;
using MonoGameEngineCore.GameObject;
using MonoGameEngineCore.GameObject.Components;
using MonoGameEngineCore.Procedural;
using MonoGameEngineCore.Rendering;
using MonoGameEngineCore.ScreenManagement;
using MonoGameEngineCore;
using MonoGameEngineCore.Helper;

namespace MonoGameDirectX11.Screens
{
    public class RenderTextureTest : TestScreen
    {

        RenderTarget2D renderTarget;
        GameObject testObject;
        private DummyCamera renderTextureCamera;
        private GameObject planeToDrawOn;
        SpriteBatch spriteBatch;
        private SpriteFont font;

        public RenderTextureTest()
            : base()
        {
          
        }

        public override void OnInitialise()
        {
            base.OnInitialise();

            SystemCore.ActiveScene.SetUpBasicAmbientAndKey();
            mouseCamera.moveSpeed = 0.01f;

            ProceduralCube cube = new ProceduralCube();
            cube.SetColor(Color.OrangeRed);
            testObject = GameObjectFactory.CreateRenderableGameObjectFromShape(cube, EffectLoader.LoadSM5Effect("flatshaded"));
            testObject.AddComponent(new RotatorComponent(Vector3.Up, 0.0001f));
            SystemCore.GameObjectManager.AddAndInitialiseGameObject(testObject);

            ProceduralPlane plane = new ProceduralPlane();
            plane.Scale(10f);
            plane.SetColor(Color.LightBlue);
            planeToDrawOn = GameObjectFactory.CreateRenderTextureSurface(plane,
                EffectLoader.LoadSM5Effect("rendertexturesurface"));

            planeToDrawOn.Transform.Rotate(Vector3.Forward, MathHelper.ToRadians(90));
            planeToDrawOn.Transform.Rotate(Vector3.Left, MathHelper.ToRadians(-90));
            //planeToDrawOn.Transform.Rotate(Vector3.Up, MathHelper.ToRadians(-90));
            planeToDrawOn.Transform.SetPosition(new Vector3(-5, 0, 0));

            GameObject.InitialiseAllComponents(planeToDrawOn);

            spriteBatch = new SpriteBatch(SystemCore.GraphicsDevice);
            renderTarget = new RenderTarget2D(SystemCore.GraphicsDevice, 500, 500);

            renderTextureCamera = new DummyCamera();
            renderTextureCamera.SetPositionAndLookDir(new Vector3(-5, 0, 0), Vector3.Zero, Vector3.Up);
            SystemCore.AddCamera("renderTextureCamera", renderTextureCamera);

            var secondCube = GameObjectFactory.CreateRenderableGameObjectFromShape(new ProceduralCube(),
            EffectLoader.LoadSM5Effect("flatshaded"));

            secondCube.Transform.SetPosition(new Vector3(-20, 0, 0));
            SystemCore.GameObjectManager.AddAndInitialiseGameObject(secondCube);

            font = SystemCore.ContentManager.Load<SpriteFont>("Fonts/neuropolitical");


          
        }

        public override void OnRemove()
        {
            SystemCore.RemoveCamera("renderTextureCamera");
            base.OnRemove();
        }

        public override void Update(GameTime gameTime)
        {

            testObject.GetComponent<RotatorComponent>().Update(gameTime);

            base.Update(gameTime);
        }


        public override void Render(GameTime gameTime)
        {

            //set render target, clear depth buffer (including alpha), draw, then reset render target.
            SystemCore.GraphicsDevice.SetRenderTarget(renderTarget);
            SystemCore.GraphicsDevice.Clear(new Color(0, 0, 0, 0));

            //testObject.GetComponent<EffectRenderComponent>().Camera = "renderTextureCamera";
            //testObject.GetComponent<EffectRenderComponent>().Draw(gameTime);

            spriteBatch.Begin();
            spriteBatch.DrawString(font, "Warning", new Vector2(renderTarget.Width/2, renderTarget.Height/2),
                Color.White);
            spriteBatch.End();
            SystemCore.GraphicsDevice.SetRenderTarget(null);




            testObject.GetComponent<EffectRenderComponent>().Camera = "main";



        }

        public override void RenderSprites(GameTime gameTime)
        {
            planeToDrawOn.GetComponent<RenderTextureComponent>().DrawOrder = 10;
            planeToDrawOn.GetComponent<RenderTextureComponent>().Texture2D = renderTarget;
            planeToDrawOn.GetComponent<RenderTextureComponent>().BorderColor = Color.OrangeRed;
            planeToDrawOn.GetComponent<RenderTextureComponent>().BorderSize = 0;
            planeToDrawOn.GetComponent<RenderTextureComponent>().Draw(gameTime);
            spriteBatch.Begin();
            spriteBatch.Draw(renderTarget, new Rectangle(0, 0, 100, 100), Color.White);
            spriteBatch.End();
        }
    }
}
