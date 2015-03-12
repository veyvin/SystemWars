using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace MonoGameEngineCore.GameObject.Components
{
    public class MouseController : IComponent, IUpdateable
    {
        float leftrightRot = MathHelper.PiOver2;
        float updownRot = MathHelper.PiOver4;
        const float rotationSpeed = 0.01f;
        public float moveSpeed = 0.1f;

        public GameObject ParentObject
        {
            get;
            set;
        }

        public MouseController()
        {

        }

        public void Initialise()
        {
            Enabled = true;
            this.inputManager = SystemCore.Input;

        }

        public bool Enabled
        {
            get;
            set;
        }

        public event EventHandler<EventArgs> EnabledChanged;

        public void Update(GameTime gameTime)
        {

            Point mouseMovement = inputManager.MouseDelta;

            if (mouseMovement.X != 0)
                ParentObject.Transform.Rotate(ParentObject.Transform.WorldMatrix.Up,
                    (float)-mouseMovement.X * rotationSpeed);

            if (mouseMovement.Y != 0)
                ParentObject.Transform.Rotate(ParentObject.Transform.WorldMatrix.Left,
                    (float) mouseMovement.Y*rotationSpeed);


            if (inputManager.IsKeyDown(Keys.Left))
            {
                ParentObject.Transform.Translate(ParentObject.Transform.WorldMatrix.Left * moveSpeed);
            }
            if (inputManager.IsKeyDown(Keys.Right))
            {
                ParentObject.Transform.Translate(-ParentObject.Transform.WorldMatrix.Left * moveSpeed);
            }
            if (inputManager.IsKeyDown(Keys.Up))
            {
                ParentObject.Transform.Translate(ParentObject.Transform.WorldMatrix.Forward * moveSpeed);
            }
            if (inputManager.IsKeyDown(Keys.Down))
            {
                ParentObject.Transform.Translate(ParentObject.Transform.WorldMatrix.Backward * moveSpeed);
            }




        }

        public int UpdateOrder
        {
            get;
            set;
        }

        public event EventHandler<EventArgs> UpdateOrderChanged;
        private InputManager inputManager;
    }
}