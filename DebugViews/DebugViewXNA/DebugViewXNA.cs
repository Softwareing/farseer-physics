﻿using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace FarseerPhysics.DebugViewXNA
{
    public class DebugViewXNA : DebugView
    {
        public DebugViewXNA(World world)
            : base(world)
        {
            _stringData = new List<StringData>();
        }

        /// Call this to draw shapes and other debug draw data.
        public void DrawDebugData()
        {
            if ((Flags & DebugViewFlags.Shape) == DebugViewFlags.Shape)
            {
                for (Body b = World.BodyList; b != null; b = b.GetNext())
                {
                    Transform xf;
                    b.GetTransform(out xf);
                    for (Fixture f = b.GetFixtureList(); f != null; f = f.GetNext())
                    {
                        if (b.IsActive() == false)
                        {
                            DrawShape(f, xf, new Color(0.5f, 0.5f, 0.3f));
                        }
                        else if (b.GetType() == BodyType.Static)
                        {
                            DrawShape(f, xf, new Color(0.5f, 0.9f, 0.5f));
                        }
                        else if (b.GetType() == BodyType.Kinematic)
                        {
                            DrawShape(f, xf, new Color(0.5f, 0.5f, 0.9f));
                        }
                        else if (b.IsAwake() == false)
                        {
                            DrawShape(f, xf, new Color(0.6f, 0.6f, 0.6f));
                        }
                        else
                        {
                            DrawShape(f, xf, new Color(0.9f, 0.7f, 0.7f));
                        }
                    }
                }
            }

            if ((Flags & DebugViewFlags.Joint) == DebugViewFlags.Joint)
            {
                for (Joint j = World.JointList; j != null; j = j.GetNext())
                {
                    DrawJoint(j);
                }
            }

            if ((Flags & DebugViewFlags.Pair) == DebugViewFlags.Pair)
            {
                Color color = new Color(0.3f, 0.9f, 0.9f);
                for (Contact c = World.ContactManager.ContactList; c != null; c = c.GetNext())
                {
                    Fixture fixtureA = c.GetFixtureA();
                    Fixture fixtureB = c.GetFixtureB();

                    AABB aabbA;
                    AABB aabbB;
                    fixtureA.GetAABB(out aabbA);
                    fixtureB.GetAABB(out aabbB);

                    Vector2 cA = aabbA.GetCenter();
                    Vector2 cB = aabbB.GetCenter();

                    DrawSegment(cA, cB, color);
                }
            }

            if ((Flags & DebugViewFlags.AABB) == DebugViewFlags.AABB)
            {
                Color color = new Color(0.9f, 0.3f, 0.9f);
                BroadPhase bp = World.ContactManager.BroadPhase;

                for (Body b = World.BodyList; b != null; b = b.GetNext())
                {
                    if (b.IsActive() == false)
                    {
                        continue;
                    }

                    for (Fixture f = b.GetFixtureList(); f != null; f = f.GetNext())
                    {
                        AABB aabb;
                        bp.GetFatAABB(f.ProxyId, out aabb);
                        FixedArray8<Vector2> vs = new FixedArray8<Vector2>();
                        vs[0] = new Vector2(aabb.lowerBound.X, aabb.lowerBound.Y);
                        vs[1] = new Vector2(aabb.upperBound.X, aabb.lowerBound.Y);
                        vs[2] = new Vector2(aabb.upperBound.X, aabb.upperBound.Y);
                        vs[3] = new Vector2(aabb.lowerBound.X, aabb.upperBound.Y);

                        DrawPolygon(ref vs, 4, color);
                    }
                }
            }

            if ((Flags & DebugViewFlags.CenterOfMass) == DebugViewFlags.CenterOfMass)
            {
                for (Body b = World.BodyList; b != null; b = b.GetNext())
                {
                    Transform xf;
                    b.GetTransform(out xf);
                    xf.Position = b.GetWorldCenter();
                    DrawTransform(ref xf);
                }
            }
        }

        private void DrawJoint(Joint joint)
        {
            Body b1 = joint.GetBodyA();
            Body b2 = joint.GetBodyB();
            Transform xf1, xf2;
            b1.GetTransform(out xf1);
            b2.GetTransform(out xf2);
            Vector2 x1 = xf1.Position;
            Vector2 x2 = xf2.Position;
            Vector2 p1 = joint.GetAnchorA();
            Vector2 p2 = joint.GetAnchorB();

            Color color = new Color(0.5f, 0.8f, 0.8f);

            switch (joint.JointType)
            {
                case JointType.Distance:
                    DrawSegment(p1, p2, color);
                    break;

                case JointType.Pulley:
                    {
                        PulleyJoint pulley = (PulleyJoint)joint;
                        Vector2 s1 = pulley.GetGroundAnchorA();
                        Vector2 s2 = pulley.GetGroundAnchorB();
                        DrawSegment(s1, p1, color);
                        DrawSegment(s2, p2, color);
                        DrawSegment(s1, s2, color);
                    }
                    break;

                case JointType.Mouse:
                    // don't draw this
                    break;

                default:
                    DrawSegment(x1, p1, color);
                    DrawSegment(p1, p2, color);
                    DrawSegment(x2, p2, color);
                    break;
            }
        }

        private void DrawShape(Fixture fixture, Transform xf, Color color)
        {
            switch (fixture.ShapeType)
            {
                case ShapeType.Circle:
                    {
                        CircleShape circle = (CircleShape)fixture.GetShape();

                        Vector2 center = MathUtils.Multiply(ref xf, circle._p);
                        float radius = circle._radius;
                        Vector2 axis = xf.R.col1;

                        DrawSolidCircle(center, radius, axis, color);
                    }
                    break;

                case ShapeType.Polygon:
                    {
                        PolygonShape poly = (PolygonShape)fixture.GetShape();
                        int vertexCount = poly._vertexCount;
                        Debug.Assert(vertexCount <= Settings.MaxPolygonVertices);
                        FixedArray8<Vector2> vertices = new FixedArray8<Vector2>();

                        for (int i = 0; i < vertexCount; ++i)
                        {
                            vertices[i] = MathUtils.Multiply(ref xf, poly._vertices[i]);
                        }

                        DrawSolidPolygon(ref vertices, vertexCount, color);
                    }
                    break;
            }
        }

        public override void DrawPolygon(ref FixedArray8<Vector2> vertices, int count, float red, float green, float blue)
        {
            DrawPolygon(ref vertices, count, new Color(red, green, blue));
        }

        public void DrawPolygon(ref FixedArray8<Vector2> vertices, int count, Color color)
        {
            for (int i = 0; i < count - 1; i++)
            {
                _vertsLines[_lineCount * 2].Position = new Vector3(vertices[i], 0.0f);
                _vertsLines[_lineCount * 2].Color = color;
                _vertsLines[_lineCount * 2 + 1].Position = new Vector3(vertices[i + 1], 0.0f);
                _vertsLines[_lineCount * 2 + 1].Color = color;
                _lineCount++;
            }

            _vertsLines[_lineCount * 2].Position = new Vector3(vertices[count - 1], 0.0f);
            _vertsLines[_lineCount * 2].Color = color;
            _vertsLines[_lineCount * 2 + 1].Position = new Vector3(vertices[0], 0.0f);
            _vertsLines[_lineCount * 2 + 1].Color = color;
            _lineCount++;
        }

        public override void DrawSolidPolygon(ref FixedArray8<Vector2> vertices, int count, float red, float green, float blue)
        {
            DrawSolidPolygon(ref vertices, count, new Color(red, green, blue), true);
        }

        public void DrawSolidPolygon(ref FixedArray8<Vector2> vertices, int count, Color color)
        {
            DrawSolidPolygon(ref vertices, count, color, true);
        }

        public void DrawSolidPolygon(ref FixedArray8<Vector2> vertices, int count, Color color, bool outline)
        {
            if (count == 2)
            {
                DrawPolygon(ref vertices, count, color);
                return;
            }

            Color colorFill = new Color(color, outline ? 0.5f : 1.0f);

            for (int i = 1; i < count - 1; i++)
            {
                _vertsFill[_fillCount * 3].Position = new Vector3(vertices[0], 0.0f);
                _vertsFill[_fillCount * 3].Color = colorFill;

                _vertsFill[_fillCount * 3 + 1].Position = new Vector3(vertices[i], 0.0f);
                _vertsFill[_fillCount * 3 + 1].Color = colorFill;

                _vertsFill[_fillCount * 3 + 2].Position = new Vector3(vertices[i + 1], 0.0f);
                _vertsFill[_fillCount * 3 + 2].Color = colorFill;

                _fillCount++;
            }

            if (outline)
            {
                DrawPolygon(ref vertices, count, color);
            }
        }

        public override void DrawCircle(Vector2 center, float radius, float red, float green, float blue)
        {
            DrawCircle(center, radius, new Color(red, green, blue));
        }

        public void DrawCircle(Vector2 center, float radius, Color color)
        {
            int segments = 16;
            double increment = Math.PI * 2.0 / (double)segments;
            double theta = 0.0;

            for (int i = 0; i < segments; i++)
            {
                Vector2 v1 = center + radius * new Vector2((float)Math.Cos(theta), (float)Math.Sin(theta));
                Vector2 v2 = center + radius * new Vector2((float)Math.Cos(theta + increment), (float)Math.Sin(theta + increment));

                _vertsLines[_lineCount * 2].Position = new Vector3(v1, 0.0f);
                _vertsLines[_lineCount * 2].Color = color;
                _vertsLines[_lineCount * 2 + 1].Position = new Vector3(v2, 0.0f);
                _vertsLines[_lineCount * 2 + 1].Color = color;
                _lineCount++;

                theta += increment;
            }
        }

        public override void DrawSolidCircle(Vector2 center, float radius, Vector2 axis, float red, float green, float blue)
        {
            DrawSolidCircle(center, radius, axis, new Color(red, green, blue));
        }

        public void DrawSolidCircle(Vector2 center, float radius, Vector2 axis, Color color)
        {
            int segments = 16;
            double increment = Math.PI * 2.0 / (double)segments;
            double theta = 0.0;

            Color colorFill = new Color(color, 0.5f);

            Vector2 v0 = center + radius * new Vector2((float)Math.Cos(theta), (float)Math.Sin(theta));
            theta += increment;

            for (int i = 1; i < segments - 1; i++)
            {
                Vector2 v1 = center + radius * new Vector2((float)Math.Cos(theta), (float)Math.Sin(theta));
                Vector2 v2 = center + radius * new Vector2((float)Math.Cos(theta + increment), (float)Math.Sin(theta + increment));

                _vertsFill[_fillCount * 3].Position = new Vector3(v0, 0.0f);
                _vertsFill[_fillCount * 3].Color = colorFill;

                _vertsFill[_fillCount * 3 + 1].Position = new Vector3(v1, 0.0f);
                _vertsFill[_fillCount * 3 + 1].Color = colorFill;

                _vertsFill[_fillCount * 3 + 2].Position = new Vector3(v2, 0.0f);
                _vertsFill[_fillCount * 3 + 2].Color = colorFill;

                _fillCount++;

                theta += increment;
            }
            DrawCircle(center, radius, color);

            DrawSegment(center, center + axis * radius, color);
        }

        public override void DrawSegment(Vector2 p1, Vector2 p2, float red, float green, float blue)
        {
            DrawSegment(p1, p2, new Color(red, green, blue));
        }

        public void DrawSegment(Vector2 p1, Vector2 p2, Color color)
        {
            _vertsLines[_lineCount * 2].Position = new Vector3(p1, 0.0f);
            _vertsLines[_lineCount * 2 + 1].Position = new Vector3(p2, 0.0f);
            _vertsLines[_lineCount * 2].Color = _vertsLines[_lineCount * 2 + 1].Color = color;
            _lineCount++;
        }

        public override void DrawTransform(ref Transform xf)
        {
            float axisScale = 0.4f;
            Vector2 p1 = xf.Position;

            Vector2 p2 = p1 + axisScale * xf.R.col1;
            DrawSegment(p1, p2, Color.Red);

            p2 = p1 + axisScale * xf.R.col2;
            DrawSegment(p1, p2, Color.Green);
        }

        public void DrawPoint(Vector2 p, float size, Color color)
        {
            FixedArray8<Vector2> verts = new FixedArray8<Vector2>();
            float hs = size / 2.0f;
            verts[0] = p + new Vector2(-hs, -hs);
            verts[1] = p + new Vector2(hs, -hs);
            verts[2] = p + new Vector2(hs, hs);
            verts[3] = p + new Vector2(-hs, hs);

            DrawSolidPolygon(ref verts, 4, color, true);
        }

        public void DrawString(int x, int y, string s, params object[] args)
        {
            _stringData.Add(new StringData(x, y, s, args));
        }

        public void FinishDrawShapes()
        {
            _device.RenderState.CullMode = CullMode.None;
            _device.RenderState.AlphaBlendEnable = true;

            if (_fillCount > 0)
                _device.DrawUserPrimitives<VertexPositionColor>(PrimitiveType.TriangleList, _vertsFill, 0, _fillCount);

            if (_lineCount > 0)
                _device.DrawUserPrimitives<VertexPositionColor>(PrimitiveType.LineList, _vertsLines, 0, _lineCount);

            _lineCount = _fillCount = 0;
        }

        public void FinishDrawString()
        {
            for (int i = 0; i < _stringData.Count; i++)
            {
                _batch.DrawString(_font, string.Format(_stringData[i].s, _stringData[i].args), new Vector2(_stringData[i].x, _stringData[i].y), new Color(0.9f, 0.6f, 0.6f));
            }

            _stringData.Clear();
        }

        public void DrawAABB(ref AABB aabb, Color color)
        {
            FixedArray8<Vector2> verts = new FixedArray8<Vector2>();
            verts[0] = new Vector2(aabb.lowerBound.X, aabb.lowerBound.Y);
            verts[1] = new Vector2(aabb.upperBound.X, aabb.lowerBound.Y);
            verts[2] = new Vector2(aabb.upperBound.X, aabb.upperBound.Y);
            verts[3] = new Vector2(aabb.lowerBound.X, aabb.upperBound.Y);

            DrawPolygon(ref verts, 4, color);
        }

        private static VertexPositionColor[] _vertsLines = new VertexPositionColor[100000];
        private static VertexPositionColor[] _vertsFill = new VertexPositionColor[100000];
        private static int _lineCount;
        private static int _fillCount;
        public static SpriteBatch _batch;
        public static SpriteFont _font;
        public static GraphicsDevice _device;

        private List<StringData> _stringData;

        private struct StringData
        {
            public StringData(int x, int y, string s, object[] args)
            {
                this.x = x;
                this.y = y;
                this.s = s;
                this.args = args;
            }

            public int x, y;
            public string s;
            public object[] args;
        }
    }
}