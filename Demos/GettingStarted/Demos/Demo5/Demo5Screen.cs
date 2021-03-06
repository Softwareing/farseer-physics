using System.Text;
using FarseerGames.FarseerPhysics;
using FarseerGames.FarseerPhysics.Collisions;
using FarseerGames.FarseerPhysics.Dynamics.Springs;
using FarseerGames.FarseerPhysics.Factories;
using FarseerGames.FarseerPhysicsDemos.Demos.DemoShare;
using FarseerGames.FarseerPhysicsDemos.DrawingSystem;
using FarseerGames.FarseerPhysicsDemos.ScreenSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace FarseerGames.FarseerPhysicsDemos.Demos.Demo5
{
    public class Demo5Screen : GameScreen
    {
        private readonly LineBrush _lineBrush = new LineBrush(1, Color.Black); //used to draw spring on mouse grab
        private Agent _agent;
        private Circles _blackCircles1;
        private Circles _blackCircles2;
        private Circles _blackCircles3;
        private Circles _blueCircles1;
        private Circles _blueCircles2;
        private Circles _blueCircles3;
        private Border _border;
        private bool _firstRun = true;
        private Circles _greenCircles1;
        private Circles _greenCircles2;
        private Circles _greenCircles3;
        private FixedLinearSpring _mousePickSpring;
        private Geom _pickedGeom;
        private Circles _redCircles1;
        private Circles _redCircles2;
        private Circles _redCircles3;

        public override void Initialize()
        {
            PhysicsSimulator = new PhysicsSimulator(new Vector2(0, 0));
            PhysicsSimulator.MaxContactsToDetect = 2;
                //for stacked objects, simultaneous collision are the bottlenecks so limit them to 2 per geometric pair.
            PhysicsSimulatorView = new PhysicsSimulatorView(PhysicsSimulator);

            base.Initialize();
        }

        public override void LoadContent()
        {
            _lineBrush.Load(ScreenManager.GraphicsDevice);

            int borderWidth = (int) (ScreenManager.ScreenHeight*.05f);
            _border = new Border(ScreenManager.ScreenWidth, ScreenManager.ScreenHeight, borderWidth,
                                 ScreenManager.ScreenCenter);
            _border.Load(ScreenManager.GraphicsDevice, PhysicsSimulator);

            _agent = new Agent(ScreenManager.ScreenCenter);
            _agent.CollisionCategory = CollisionCategories.Cat5;
            _agent.CollidesWith = CollisionCategories.All & ~CollisionCategories.Cat4;
            //collide with all but Cat4 (black)
            _agent.Load(ScreenManager.GraphicsDevice, PhysicsSimulator);

            LoadCircles();

            base.LoadContent();
        }

        private void LoadCircles()
        {
            //Cat1=Red, Cat2=Green, Cat3=Blue, Cat4=Black, Cat5=Agent
            Vector2 startPosition = new Vector2(100, 100);
            Vector2 endPosition = new Vector2(100, ScreenManager.ScreenHeight - 100);
            _redCircles1 = new Circles(startPosition, endPosition, 15, 15, new Color(200, 0, 0, 175), Color.Black);
            _redCircles1.CollisionCategories = (CollisionCategories.Cat1);
            _redCircles1.CollidesWith = (CollisionCategories.Cat1 | CollisionCategories.Cat4 |
                                         CollisionCategories.Cat5);
            _redCircles1.Load(ScreenManager.GraphicsDevice, PhysicsSimulator);

            startPosition = new Vector2(200, 200);
            endPosition = new Vector2(200, ScreenManager.ScreenHeight - 200);
            _redCircles2 = new Circles(startPosition, endPosition, 15, 12, new Color(200, 0, 0, 175), Color.Black);
            _redCircles2.CollisionCategories = (CollisionCategories.Cat1);
            _redCircles2.CollidesWith = (CollisionCategories.Cat1 | CollisionCategories.Cat4 |
                                         CollisionCategories.Cat5);
            _redCircles2.Load(ScreenManager.GraphicsDevice, PhysicsSimulator);

            startPosition = new Vector2(300, 300);
            endPosition = new Vector2(300, ScreenManager.ScreenHeight - 300);
            _redCircles3 = new Circles(startPosition, endPosition, 10, 9, new Color(200, 0, 0, 175), Color.Black);
            _redCircles3.CollisionCategories = (CollisionCategories.Cat1);
            _redCircles3.CollidesWith = (CollisionCategories.Cat1 | CollisionCategories.Cat4 |
                                         CollisionCategories.Cat5);
            _redCircles3.Load(ScreenManager.GraphicsDevice, PhysicsSimulator);

            startPosition = new Vector2(200, 100);
            endPosition = new Vector2(ScreenManager.ScreenWidth - 200, 100);
            _greenCircles1 = new Circles(startPosition, endPosition, 15, 15, new Color(0, 200, 0, 175), Color.Black);
            _greenCircles1.CollisionCategories = (CollisionCategories.Cat2);
            _greenCircles1.CollidesWith = (CollisionCategories.Cat2 | CollisionCategories.Cat4 |
                                           CollisionCategories.Cat5);
            _greenCircles1.Load(ScreenManager.GraphicsDevice, PhysicsSimulator);

            startPosition = new Vector2(300, 200);
            endPosition = new Vector2(ScreenManager.ScreenWidth - 300, 200);
            _greenCircles2 = new Circles(startPosition, endPosition, 15, 12, new Color(0, 200, 0, 175), Color.Black);
            _greenCircles2.CollisionCategories = (CollisionCategories.Cat2);
            _greenCircles2.CollidesWith = (CollisionCategories.Cat2 | CollisionCategories.Cat4 |
                                           CollisionCategories.Cat5);
            _greenCircles2.Load(ScreenManager.GraphicsDevice, PhysicsSimulator);

            startPosition = new Vector2(400, 300);
            endPosition = new Vector2(ScreenManager.ScreenWidth - 400, 300);
            _greenCircles3 = new Circles(startPosition, endPosition, 10, 9, new Color(0, 200, 0, 175), Color.Black);
            _greenCircles3.CollisionCategories = (CollisionCategories.Cat2);
            _greenCircles3.CollidesWith = (CollisionCategories.Cat2 | CollisionCategories.Cat4 |
                                           CollisionCategories.Cat5);
            _greenCircles3.Load(ScreenManager.GraphicsDevice, PhysicsSimulator);

            startPosition = new Vector2(ScreenManager.ScreenWidth - 100, 100);
            endPosition = new Vector2(ScreenManager.ScreenWidth - 100, ScreenManager.ScreenHeight - 100);
            _blueCircles1 = new Circles(startPosition, endPosition, 15, 15, new Color(0, 0, 200, 175), Color.Black);
            _blueCircles1.CollisionCategories = (CollisionCategories.Cat3);
            _blueCircles1.CollidesWith = (CollisionCategories.Cat3 | CollisionCategories.Cat4 |
                                          CollisionCategories.Cat5);
            _blueCircles1.Load(ScreenManager.GraphicsDevice, PhysicsSimulator);

            startPosition = new Vector2(ScreenManager.ScreenWidth - 200, 200);
            endPosition = new Vector2(ScreenManager.ScreenWidth - 200, ScreenManager.ScreenHeight - 200);
            _blueCircles2 = new Circles(startPosition, endPosition, 15, 12, new Color(0, 0, 200, 175), Color.Black);
            _blueCircles2.CollisionCategories = (CollisionCategories.Cat3);
            _blueCircles2.CollidesWith = (CollisionCategories.Cat3 | CollisionCategories.Cat4 |
                                          CollisionCategories.Cat5);
            _blueCircles2.Load(ScreenManager.GraphicsDevice, PhysicsSimulator);

            startPosition = new Vector2(ScreenManager.ScreenWidth - 300, 300);
            endPosition = new Vector2(ScreenManager.ScreenWidth - 300, ScreenManager.ScreenHeight - 300);
            _blueCircles3 = new Circles(startPosition, endPosition, 10, 9, new Color(0, 0, 200, 175), Color.Black);
            _blueCircles3.CollisionCategories = (CollisionCategories.Cat3);
            _blueCircles3.CollidesWith = (CollisionCategories.Cat3 | CollisionCategories.Cat4 |
                                          CollisionCategories.Cat5);
            _blueCircles3.Load(ScreenManager.GraphicsDevice, PhysicsSimulator);

            startPosition = new Vector2(200, ScreenManager.ScreenHeight - 100);
            endPosition = new Vector2(ScreenManager.ScreenWidth - 200, ScreenManager.ScreenHeight - 100);
            _blackCircles1 = new Circles(startPosition, endPosition, 15, 15, new Color(0, 0, 0, 200), Color.Black);
            _blackCircles1.CollisionCategories = CollisionCategories.Cat4;
            _blackCircles1.CollidesWith = CollisionCategories.All & ~CollisionCategories.Cat5;
            //Collide with all but Cat5
            _blackCircles1.Load(ScreenManager.GraphicsDevice, PhysicsSimulator);

            startPosition = new Vector2(300, ScreenManager.ScreenHeight - 200);
            endPosition = new Vector2(ScreenManager.ScreenWidth - 300, ScreenManager.ScreenHeight - 200);
            _blackCircles2 = new Circles(startPosition, endPosition, 15, 12, new Color(0, 0, 0, 200), Color.Black);
            _blackCircles2.CollisionCategories = CollisionCategories.Cat4;
            _blackCircles2.CollidesWith = CollisionCategories.All & ~CollisionCategories.Cat5;
            //Collide with all but Cat5
            _blackCircles2.Load(ScreenManager.GraphicsDevice, PhysicsSimulator);

            startPosition = new Vector2(400, ScreenManager.ScreenHeight - 300);
            endPosition = new Vector2(ScreenManager.ScreenWidth - 400, ScreenManager.ScreenHeight - 300);
            _blackCircles3 = new Circles(startPosition, endPosition, 10, 9, new Color(0, 0, 0, 200), Color.Black);
            _blackCircles3.CollisionCategories = CollisionCategories.Cat4;
            _blackCircles3.CollidesWith = CollisionCategories.All & ~CollisionCategories.Cat5;
            //Collide with all but Cat5
            _blackCircles3.Load(ScreenManager.GraphicsDevice, PhysicsSimulator);
        }

        public override void Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen)
        {
            if (IsActive)
            {
                PhysicsSimulator.Update(gameTime.ElapsedGameTime.Milliseconds*.001f);
            }
            base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);
        }

        public override void Draw(GameTime gameTime)
        {
            ScreenManager.SpriteBatch.Begin(SpriteBlendMode.AlphaBlend);
            _border.Draw(ScreenManager.SpriteBatch);
            _agent.Draw(ScreenManager.SpriteBatch);

            _redCircles1.Draw(ScreenManager.SpriteBatch);
            _redCircles2.Draw(ScreenManager.SpriteBatch);
            _redCircles3.Draw(ScreenManager.SpriteBatch);

            _greenCircles1.Draw(ScreenManager.SpriteBatch);
            _greenCircles2.Draw(ScreenManager.SpriteBatch);
            _greenCircles3.Draw(ScreenManager.SpriteBatch);

            _blueCircles1.Draw(ScreenManager.SpriteBatch);
            _blueCircles2.Draw(ScreenManager.SpriteBatch);
            _blueCircles3.Draw(ScreenManager.SpriteBatch);

            _blackCircles1.Draw(ScreenManager.SpriteBatch);
            _blackCircles2.Draw(ScreenManager.SpriteBatch);
            _blackCircles3.Draw(ScreenManager.SpriteBatch);

            if (_mousePickSpring != null)
            {
                _lineBrush.Draw(ScreenManager.SpriteBatch,
                                _mousePickSpring.Body.GetWorldPosition(_mousePickSpring.BodyAttachPoint),
                                _mousePickSpring.WorldAttachPoint);
            }

            ScreenManager.SpriteBatch.End();

            base.Draw(gameTime);
        }

        public override void HandleInput(InputState input)
        {
            if (_firstRun)
            {
                ScreenManager.AddScreen(new PauseScreen(GetTitle(), GetDetails()));
                _firstRun = false;
            }

            if (input.PauseGame)
            {
                ScreenManager.AddScreen(new PauseScreen(GetTitle(), GetDetails()));
            }

            HandleKeyboardInput(input);
            HandleMouseInput(input);

            base.HandleInput(input);
        }

        private void HandleKeyboardInput(InputState input)
        {
            const float forceAmount = 1000;
            Vector2 force = Vector2.Zero;
            force.Y = -force.Y;
            if (input.CurrentKeyboardState.IsKeyDown(Keys.A))
            {
                force += new Vector2(-forceAmount, 0);
            }
            if (input.CurrentKeyboardState.IsKeyDown(Keys.S))
            {
                force += new Vector2(0, forceAmount);
            }
            if (input.CurrentKeyboardState.IsKeyDown(Keys.D))
            {
                force += new Vector2(forceAmount, 0);
            }
            if (input.CurrentKeyboardState.IsKeyDown(Keys.W))
            {
                force += new Vector2(0, -forceAmount);
            }

            _agent.ApplyForce(force);

            const float torqueAmount = 14000;
            float torque = 0;
            if (input.CurrentKeyboardState.IsKeyDown(Keys.Left))
            {
                torque -= torqueAmount;
            }
            if (input.CurrentKeyboardState.IsKeyDown(Keys.Right))
            {
                torque += torqueAmount;
            }
            _agent.ApplyTorque(torque);
        }

        private void HandleMouseInput(InputState input)
        {
            Vector2 point = new Vector2(input.CurrentMouseState.X, input.CurrentMouseState.Y);
            if (input.LastMouseState.LeftButton == ButtonState.Released &&
                input.CurrentMouseState.LeftButton == ButtonState.Pressed)
            {
                //create mouse spring
                _pickedGeom = PhysicsSimulator.Collide(point);
                if (_pickedGeom != null)
                {
                    _mousePickSpring = ControllerFactory.Instance.CreateFixedLinearSpring(PhysicsSimulator,
                                                                                          _pickedGeom.Body,
                                                                                          _pickedGeom.Body.
                                                                                              GetLocalPosition(point),
                                                                                          point, 20, 10);
                }
            }
            else if (input.LastMouseState.LeftButton == ButtonState.Pressed &&
                     input.CurrentMouseState.LeftButton == ButtonState.Released)
            {
                //destroy mouse spring
                if (_mousePickSpring != null && _mousePickSpring.IsDisposed == false)
                {
                    _mousePickSpring.Dispose();
                    _mousePickSpring = null;
                }
            }

            //move anchor point
            if (input.CurrentMouseState.LeftButton == ButtonState.Pressed && _mousePickSpring != null)
            {
                _mousePickSpring.WorldAttachPoint = point;
            }
        }

        public string GetTitle()
        {
            return "Collision Categories";
        }

        public string GetDetails()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("This demo shows how to setup complex collision");
            sb.AppendLine("scenerios.");
            sb.AppendLine("In this demo:");
            sb.AppendLine("-Red, Green, and Blue are set to only collide with");
            sb.AppendLine(" their own color.");
            sb.AppendLine("-Black is set to collide with itself, Red, Green, ");
            sb.AppendLine(" and Blue.");
            sb.AppendLine("-The 'Agent' (the cross thing) is set to collide");
            sb.AppendLine(" with all but Black");
            sb.AppendLine(string.Empty);
            sb.AppendLine("NOTE: If two objects define conflicting");
            sb.AppendLine("collision status, collide wins over not colliding.");
            sb.AppendLine("This is the case with Black vs. the Red, Green, ");
            sb.AppendLine("and Blue circles");
            sb.AppendLine(string.Empty);
            sb.AppendLine(string.Empty);
            sb.AppendLine("Keyboard:");
            sb.AppendLine("  -Rotate : left and right arrows");
            sb.AppendLine("  -Move : A,S,D,W");
            sb.AppendLine(string.Empty);
            sb.AppendLine("Mouse");
            sb.AppendLine("  -Hold down left button and drag");
            return sb.ToString();
        }
    }
}