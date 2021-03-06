using System;
using System.Diagnostics;
using FarseerGames.FarseerPhysics.Mathematics;
using Microsoft.Xna.Framework;

namespace FarseerGames.FarseerPhysics.Dynamics.Joints
{
    public class RevoluteJoint : Joint
    {
        private Vector2 _accumulatedImpulse;
        private Vector2 _anchor;
        private Matrix _b;
        private float _biasFactor = .2f;

        private float _breakpoint = float.MaxValue;
        private Vector2 _currentAnchor;
        private Vector2 _dv;
        private Vector2 _dvBias;
        private Vector2 _impulse;

        private float _jointError;
        private Matrix _matrix;
        private Vector2 _r1;
        private Vector2 _r2;
        private float _softness;
        private Vector2 _velocityBias;
        private Body _body1;
        private Body _body2;
        private Vector2 _localAnchor1;
        private Vector2 _localAnchor2;

        public RevoluteJoint()
        {
        }

        public RevoluteJoint(Body body1, Body body2, Vector2 anchor)
        {
            _body1 = body1;
            _body2 = body2;

            _anchor = anchor;

            body1.GetLocalPosition(ref anchor, out _localAnchor1);
            body2.GetLocalPosition(ref anchor, out _localAnchor2);

            _accumulatedImpulse = Vector2.Zero;
        }

        public Body Body1
        {
            get { return _body1; }
            set { _body1 = value; }
        }

        public Body Body2
        {
            get { return _body2; }
            set { _body2 = value; }
        }

        public float BiasFactor
        {
            get { return _biasFactor; }
            set { _biasFactor = value; }
        }

        public float Softness
        {
            get { return _softness; }
            set { _softness = value; }
        }

        public float Breakpoint
        {
            get { return _breakpoint; }
            set { _breakpoint = value; }
        }

        public float JointError
        {
            get { return _jointError; }
        }

        public Vector2 Anchor
        {
            get { return _anchor; }
            set
            {
                _anchor = value;
                _body1.GetLocalPosition(ref _anchor, out _localAnchor1);
                _body2.GetLocalPosition(ref _anchor, out _localAnchor2);
            }
        }

        //this gives the _anchor position after the simulation starts
        public Vector2 CurrentAnchor
        {
            get
            {
                Vector2.Add(ref _body1.position, ref _r1, out _currentAnchor); //_anchor moves once simulator starts
                return _currentAnchor;
            }
        }

        public event EventHandler<EventArgs> Broke;

        public void SetInitialAnchor(Vector2 initialAnchor)
        {
            _anchor = initialAnchor;
            if (_body1 == null)
            {
                throw new ArgumentNullException("initialAnchor", "Body must be set prior to setting the _anchor of the Revolute Joint");
            }
            _body1.GetLocalPosition(ref initialAnchor, out _localAnchor1);
            _body2.GetLocalPosition(ref initialAnchor, out _localAnchor2);
        }

        public override void Validate()
        {
            if (_body1.IsDisposed || _body2.IsDisposed)
            {
                Dispose();
            }
        }

        public override void PreStep(float inverseDt)
        {
            if (Enabled && Math.Abs(_jointError) > _breakpoint)
            {
                Enabled = false;
                if (Broke != null) Broke(this, new EventArgs());
            }
            if (IsDisposed) return;

            _body1InverseMass = _body1.inverseMass;
            _body1InverseMomentOfInertia = _body1.inverseMomentOfInertia;

            _body2InverseMass = _body2.inverseMass;
            _body2InverseMomentOfInertia = _body2.inverseMomentOfInertia;

            _body1.GetBodyMatrix(out _body1MatrixTemp);
            _body2.GetBodyMatrix(out _body2MatrixTemp);
            Vector2.TransformNormal(ref _localAnchor1, ref _body1MatrixTemp, out _r1);
            Vector2.TransformNormal(ref _localAnchor2, ref _body2MatrixTemp, out _r2);

            _k1.M11 = _body1InverseMass + _body2InverseMass;
            _k1.M12 = 0;
            _k1.M21 = 0;
            _k1.M22 = _body1InverseMass + _body2InverseMass;

            _k2.M11 = _body1InverseMomentOfInertia*_r1.Y*_r1.Y;
            _k2.M12 = -_body1InverseMomentOfInertia*_r1.X*_r1.Y;
            _k2.M21 = -_body1InverseMomentOfInertia*_r1.X*_r1.Y;
            _k2.M22 = _body1InverseMomentOfInertia*_r1.X*_r1.X;

            _k3.M11 = _body2InverseMomentOfInertia*_r2.Y*_r2.Y;
            _k3.M12 = -_body2InverseMomentOfInertia*_r2.X*_r2.Y;
            _k3.M21 = -_body2InverseMomentOfInertia*_r2.X*_r2.Y;
            _k3.M22 = _body2InverseMomentOfInertia*_r2.X*_r2.X;

            //Matrix _k = _k1 + _k2 + _k3;
            Matrix.Add(ref _k1, ref _k2, out _k);
            Matrix.Add(ref _k, ref _k3, out _k);

            _k.M11 += _softness;
            _k.M12 += _softness;

            //_matrix = MatrixInvert2D(_k);
            MatrixInvert2D(ref _k, out _matrix);

            Vector2.Add(ref _body1.position, ref _r1, out _vectorTemp1);
            Vector2.Add(ref _body2.position, ref _r2, out _vectorTemp2);
            Vector2.Subtract(ref _vectorTemp2, ref _vectorTemp1, out _vectorTemp3);
            Vector2.Multiply(ref _vectorTemp3, -_biasFactor*inverseDt, out _velocityBias);

            _jointError = _vectorTemp3.Length();

            _body2.ApplyImmediateImpulse(ref _accumulatedImpulse);
            Calculator.Cross(ref _r2, ref _accumulatedImpulse, out _floatTemp1);
            _body2.ApplyAngularImpulse(_floatTemp1);

            Vector2.Multiply(ref _accumulatedImpulse, -1, out _vectorTemp1);
            _body1.ApplyImmediateImpulse(ref _vectorTemp1);
            Calculator.Cross(ref _r1, ref _accumulatedImpulse, out _floatTemp1);
            _body1.ApplyAngularImpulse(-_floatTemp1);
        }

        private void MatrixInvert2D(ref Matrix matrix, out Matrix invertedMatrix)
        {
            float a = matrix.M11, b = matrix.M12, c = matrix.M21, d = matrix.M22;
            float det = a*d - b*c;
            Debug.Assert(det != 0.0f);
            det = 1.0f/det;
            _b.M11 = det*d;
            _b.M12 = -det*b;
            _b.M21 = -det*c;
            _b.M22 = det*a;
            invertedMatrix = _b;
        }

        public override void Update()
        {
            //if (Math.Abs(_jointError) > _breakpoint) { Dispose(); } //check if joint is broken
            if (IsDisposed) return;
            if (!Enabled) return;

            //Vector2 _dv = _body2.LinearVelocity + Calculator.Cross(_body2.AngularVelocity, _r2) - _body1.LinearVelocity - Calculator.Cross(_body1.AngularVelocity, _r1);

            #region INLINE: Calculator.Cross(ref _body2.angularVelocity, ref _r2, out _vectorTemp1);

            _vectorTemp1.X = -_body2.angularVelocity*_r2.Y;
            _vectorTemp1.Y = _body2.angularVelocity*_r2.X;

            #endregion

            #region INLINE: Calculator.Cross(ref _body1.angularVelocity, ref _r1, out _vectorTemp2);

            _vectorTemp2.X = -_body1.angularVelocity*_r1.Y;
            _vectorTemp2.Y = _body1.angularVelocity*_r1.X;

            #endregion

            #region INLINE: Vector2.Add(ref _body2.linearVelocity, ref _vectorTemp1, out _vectorTemp3);

            _vectorTemp3.X = _body2.linearVelocity.X + _vectorTemp1.X;
            _vectorTemp3.Y = _body2.linearVelocity.Y + _vectorTemp1.Y;

            #endregion

            #region INLINE: Vector2.Add(ref _body1.linearVelocity, ref _vectorTemp2, out _vectorTemp4);

            _vectorTemp4.X = _body1.linearVelocity.X + _vectorTemp2.X;
            _vectorTemp4.Y = _body1.linearVelocity.Y + _vectorTemp2.Y;

            #endregion

            #region INLINE: Vector2.Subtract(ref _vectorTemp3, ref _vectorTemp4, out _dv);

            _dv.X = _vectorTemp3.X - _vectorTemp4.X;
            _dv.Y = _vectorTemp3.Y - _vectorTemp4.Y;

            #endregion

            #region INLINE: Vector2.Subtract(ref _velocityBias, ref _dv, out _vectorTemp1);

            _vectorTemp1.X = _velocityBias.X - _dv.X;
            _vectorTemp1.Y = _velocityBias.Y - _dv.Y;

            #endregion

            #region INLINE: Vector2.Multiply(ref _accumulatedImpulse, _softness, out _vectorTemp2);

            _vectorTemp2.X = _accumulatedImpulse.X*_softness;
            _vectorTemp2.Y = _accumulatedImpulse.Y*_softness;

            #endregion

            #region INLINE: Vector2.Subtract(ref _vectorTemp1, ref _vectorTemp2, out _dvBias);

            _dvBias.X = _vectorTemp1.X - _vectorTemp2.X;
            _dvBias.Y = _vectorTemp1.Y - _vectorTemp2.Y;

            #endregion

            #region INLINE: Vector2.Transform(ref _dvBias, ref _matrix, out _impulse);

            float num2 = ((_dvBias.X*_matrix.M11) + (_dvBias.Y*_matrix.M21)) + _matrix.M41;
            float num = ((_dvBias.X*_matrix.M12) + (_dvBias.Y*_matrix.M22)) + _matrix.M42;
            _impulse.X = num2;
            _impulse.Y = num;

            #endregion

            #region INLINE: _body2.ApplyImpulse(ref _impulse);

            _dv.X = _impulse.X*_body2.inverseMass;
            _dv.Y = _impulse.Y*_body2.inverseMass;

            _body2.linearVelocity.X = _dv.X + _body2.linearVelocity.X;
            _body2.linearVelocity.Y = _dv.Y + _body2.linearVelocity.Y;

            #endregion

            #region INLINE: Calculator.Cross(ref _r2, ref _impulse, out _floatTemp1);

            _floatTemp1 = _r2.X*_impulse.Y - _r2.Y*_impulse.X;

            #endregion

            #region INLINE: _body2.ApplyAngularImpulse(ref _floatTemp1);

            _body2.angularVelocity += _floatTemp1*_body2.inverseMomentOfInertia;

            #endregion

            #region INLINE: Vector2.Multiply(ref _impulse, -1, out _vectorTemp1);

            _vectorTemp1.X = _impulse.X*-1;
            _vectorTemp1.Y = _impulse.Y*-1;

            #endregion

            #region INLINE: _body1.ApplyImpulse(ref _vectorTemp1);

            _dv.X = _vectorTemp1.X*_body1.inverseMass;
            _dv.Y = _vectorTemp1.Y*_body1.inverseMass;

            _body1.linearVelocity.X = _dv.X + _body1.linearVelocity.X;
            _body1.linearVelocity.Y = _dv.Y + _body1.linearVelocity.Y;

            #endregion

            #region INLINE: Calculator.Cross(ref _r1, ref _impulse, out _floatTemp1);

            _floatTemp1 = _r1.X*_impulse.Y - _r1.Y*_impulse.X;

            #endregion

            _floatTemp1 = -_floatTemp1;

            #region INLINE: _body1.ApplyAngularImpulse(ref _floatTemp1);

            _body1.angularVelocity += _floatTemp1*_body1.inverseMomentOfInertia;

            #endregion

            #region INLINE: Vector2.Add(ref _accumulatedImpulse, ref _impulse, out _accumulatedImpulse);

            _accumulatedImpulse.X = _accumulatedImpulse.X + _impulse.X;
            _accumulatedImpulse.Y = _accumulatedImpulse.Y + _impulse.Y;

            #endregion
        }

        #region PreStep variables

        private float _body1InverseMass;
        private float _body1InverseMomentOfInertia;
        private Matrix _body1MatrixTemp;
        private float _body2InverseMass;
        private float _body2InverseMomentOfInertia;
        private Matrix _body2MatrixTemp;
        private float _floatTemp1;
        private Matrix _k;
        private Matrix _k1;
        private Matrix _k2;
        private Matrix _k3;
        private Vector2 _vectorTemp1 = Vector2.Zero;
        private Vector2 _vectorTemp2 = Vector2.Zero;
        private Vector2 _vectorTemp3 = Vector2.Zero;
        private Vector2 _vectorTemp4 = Vector2.Zero;

        #endregion
    }
}