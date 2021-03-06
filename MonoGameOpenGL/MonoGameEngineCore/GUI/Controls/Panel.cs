using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameEngineCore;


namespace MonoGameEngineCore.GUI.Controls
{
    public class Panel : BaseControl
    {
        public Texture2D Texture { get; set; }
        public Rectangle Rect { get; set; }
        public Color MainColor { get; set; }
        public Color HighlightColor { get; set; }

        public bool HighlightOnMouseOver;
        public TimeSpan HoverOverTime { get; set; }
        public bool Border { get; set; }
        public Color BorderColor { get; set; }
        public int BorderThickness { get; set; }

        public Panel(Rectangle rec, Texture2D texture)
        {
            HighlightAlpha = 1;
            MainAlpha = 1;
            MainColor = Color.White;
            Rect = rec;
            Texture = texture;
        }


        public override void Update(GameTime gameTime, InputManager input)
        {


            if (input.MouseInRectangle(Rect) && Visible)
            {
                if (MouseOver == false)
                {
                    if (base.OnMouseEnterEvent != null)
                        base.OnMouseEnterEvent(this, null);
                }

                MouseOver = true;
            }
            else
            {
                if (MouseOver)
                {
                    if (base.OnMouseLeaveEvent != null)
                        base.OnMouseLeaveEvent(this, null);
                }
                MouseOver = false;
            }




            if (Focusable)
            {
                if (MouseOver)
                {
                    if (input.MouseLeftPress() && !HasFocus)
                    {
                        HasFocus = true;
                        if (OnFocusEvent != null)
                            OnFocusEvent(this, null);

                        return;
                    }
                }
                else
                {
                    if (input.MouseLeftPress() && HasFocus)
                    {
                        HasFocus = false;
                        if (OnFocusLostEvent != null)
                            OnFocusLostEvent(this, null);

                        return;
                    }
                }



            }

            if (MouseOver)
            {
                HoverOverTime += new TimeSpan(0, 0, 0, 0, gameTime.ElapsedGameTime.Milliseconds);
            }
            else
            {
                HoverOverTime = new TimeSpan();
            }

            base.Update(gameTime, input);
        }

        public override void Draw(GameTime gameTime, SpriteBatch spriteBatch, GraphicsDevice device)
        {

            if (MouseOver && HighlightOnMouseOver)
                spriteBatch.Draw(Texture, Rect, HighlightColor*HighlightAlpha);
            else
                spriteBatch.Draw(Texture, Rect, MainColor * MainAlpha);


            if (Border)
            {
                DrawBorder(spriteBatch);
            }

            base.Draw(gameTime, spriteBatch, device);
        }

        public void DrawBorder(SpriteBatch spriteBatch)
        {

            Rectangle topBorder = new Rectangle(Rect.X - BorderThickness, Rect.Y - BorderThickness, Rect.Width + BorderThickness * 2, BorderThickness);
            spriteBatch.Draw(GUITexture.Textures["blank"], topBorder, BorderColor * MainAlpha);

            Rectangle bottomBorder = new Rectangle(Rect.X - BorderThickness, Rect.Y + Rect.Height, Rect.Width + BorderThickness, BorderThickness);
            spriteBatch.Draw(GUITexture.Textures["blank"], bottomBorder, BorderColor * MainAlpha);

            Rectangle leftBorder = new Rectangle(Rect.X - BorderThickness, Rect.Y - BorderThickness, BorderThickness, Rect.Height + BorderThickness * 2);
            spriteBatch.Draw(GUITexture.Textures["blank"], leftBorder, BorderColor * MainAlpha);


            Rectangle rightBorder = new Rectangle(Rect.X + Rect.Width, Rect.Y - BorderThickness, BorderThickness, Rect.Height + BorderThickness * 2);
            spriteBatch.Draw(GUITexture.Textures["blank"], rightBorder, BorderColor * MainAlpha);
        }

        public override void Translate(Vector2 offset)
        {
            Rect = new Rectangle(Rect.X + (int)offset.X, Rect.Y + (int)offset.Y, Rect.Width, Rect.Height);
            base.Translate(offset);
        }

        public override void SetPosition(Vector2 pos)
        {
            Rect = new Rectangle((int)pos.X, (int)pos.Y, Rect.Width, Rect.Height);
        }

        public override void SetPalette(Palette palette)
        {

            MainColor = palette.MainColor;
            HighlightColor = palette.HighlightColor;
            BorderColor = palette.BorderColor;
            Border = palette.Border;
            BorderThickness = palette.BorderThickness;

            foreach (BaseControl b in Children)
                b.SetPalette(palette);


        }

        public override void SetPaletteSecondary(Palette palette)
        {
            MainColor = palette.SecondaryColor;
            HighlightColor = palette.HighlightColor;
            BorderColor = palette.BorderColor;
            Border = palette.Border;
            BorderThickness = palette.BorderThickness;

            foreach (BaseControl b in Children)
                b.SetPaletteSecondary(palette);
        }

        public override void Anchor(AnchorPoint anchorPoint, GUIManager.ScreenPoint screenPoint, Vector2 offset)
        {
            Vector2 topLeftDiff = GetAnchorOffset(anchorPoint);
            Vector2 screenAnchor = SystemCore.GUIManager.GetScreenPoint(screenPoint);
            screenAnchor += topLeftDiff;
            screenAnchor += offset;
            Rect = new Rectangle((int)screenAnchor.X, (int)screenAnchor.Y, Rect.Width, Rect.Height);

        }

        public override void Anchor(AnchorPoint anchorPoint, GUIManager.ScreenPoint screenPoint, float xScreenRatioOffset, float yScreenRatioOffset)
        {
            int xOffset = GUIManager.ScreenRatioX(xScreenRatioOffset);
            int yOffset = GUIManager.ScreenRatioY(yScreenRatioOffset);
            Anchor(anchorPoint, screenPoint, new Vector2(xOffset, yOffset));
        }

        public  override Vector2 GetAnchorOffset(AnchorPoint anchorPoint)
        {
            switch (anchorPoint)
            {
                case (AnchorPoint.topLeft):
                    return Vector2.Zero;
                    break;
                case (AnchorPoint.topMid):
                    return new Vector2(-Rect.Width / 2, 0);
                    break;
                case (AnchorPoint.topRight):
                    return new Vector2(-Rect.Width, 0);
                    break;
                case (AnchorPoint.midRight):
                    return new Vector2(-Rect.Width, -Rect.Height / 2);
                    break;
                case (AnchorPoint.bottomRight):
                    return new Vector2(-Rect.Width, -Rect.Height);
                    break;
                case (AnchorPoint.bottomMid):
                    return new Vector2(-Rect.Width / 2, -Rect.Height);
                    break;
                case (AnchorPoint.bottomLeft):
                    return new Vector2(0, -Rect.Height);
                    break;
                case (AnchorPoint.midLeft):
                    return new Vector2(0, -Rect.Height / 2);
                    break;
                case (AnchorPoint.middle):
                    return new Vector2(-Rect.Width / 2, -Rect.Height / 2);
                    break;
            }

            return Vector2.Zero;

        }

       

    }
}
