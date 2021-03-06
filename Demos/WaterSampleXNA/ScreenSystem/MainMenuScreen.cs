using FarseerGames.WaterSampleXNA.Demos;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FarseerGames.WaterSampleXNA.ScreenSystem
{
    /// <summary>
    /// The main menu screen is the first thing displayed when the game starts up.
    /// </summary>
    internal class MainMenuScreen : MenuScreen
    {
        /// <summary>
        /// Constructor fills in the menu contents.
        /// </summary>
        public MainMenuScreen()
        {
            MenuEntries.Add(Demo1Screen.GetTitle());
            //MenuEntries.Add(Demo2Screen.GetTitle());
            //MenuEntries.Add(Demo3Screen.GetTitle());
            //MenuEntries.Add(Demo4Screen.GetTitle());
            //MenuEntries.Add(Demo5Screen.GetTitle());
            //MenuEntries.Add(Demo6Screen.GetTitle());
            //MenuEntries.Add(Demo7Screen.GetTitle());
            //MenuEntries.Add(Demo8Screen.GetTitle());
            MenuEntries.Add("Exit");
            LeftBorder = 100;
        }

        /// <summary>
        /// Responds to user menu selections.
        /// </summary>
        protected override void OnSelectEntry(int entryIndex)
        {
            switch (entryIndex)
            {
                case 0:
                    ScreenManager.AddScreen(new Demo1Screen());
                    break;
                    //case 1:
                    //    ScreenManager.AddScreen(new Demo2Screen());
                    //    break;
                    //case 2:
                    //    ScreenManager.AddScreen(new Demo3Screen());
                    //    break;
                    //case 3:
                    //    ScreenManager.AddScreen(new Demo4Screen());
                    //    break;
                    //case 4:
                    //    ScreenManager.AddScreen(new Demo5Screen());
                    //    break;
                    //case 5:
                    //    ScreenManager.AddScreen(new Demo6Screen());
                    //    break;
                    //case 6:
                    //    ScreenManager.AddScreen(new Demo7Screen());
                    //    break;
                    //case 7:
                    //    ScreenManager.AddScreen(new Demo8Screen());
                    //    break;
                case 8:
                    // Exit the sample.
                    ScreenManager.Game.Exit();
                    break;
            }

            ExitScreen();
        }

        public override void Draw(GameTime gameTime)
        {
            ScreenManager.SpriteBatch.Begin(SpriteBlendMode.AlphaBlend);
            ScreenManager.SpriteBatch.DrawString(ScreenManager.SpriteFonts.DiagnosticSpriteFont,
                                                 "1) Toggle between debug and normal view using either F1 on the keyboard or 'Y' on the controller",
                                                 new Vector2(100, ScreenManager.ScreenHeight - 116), Color.White);
            ScreenManager.SpriteBatch.DrawString(ScreenManager.SpriteFonts.DiagnosticSpriteFont,
                                                 "2) Keyboard users, use arrows and enter to navigate menus",
                                                 new Vector2(100, ScreenManager.ScreenHeight - 100), Color.White);
            base.Draw(gameTime);
            ScreenManager.SpriteBatch.End();
        }
    }
}