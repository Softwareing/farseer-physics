using System;
using FarseerGames.FarseerPhysics.Mathematics;
using Microsoft.Xna.Framework;

namespace FarseerGames.FarseerPhysics.Dynamics.Joints
{
    public class SliderJoint : Joint
    {
        private float _accumulatedImpulse;
        private Vector2 _anchor;
        private Vector2 _anchor1;
        private Vector2 _anchor2;
        private float _biasFactor = .2f;

        private float _breakpoint = float.MaxValue;
        private float _effectiveMass;

        private float _jointError;
        private bool _lowerLimitViolated;
        private float _max;
        private float _min;
        private Vector2 _r1;
        private Vector2 _r2;
        private float _slop = .01f;
        private float _softness;
        private bool _upperLimitViolated;
        private float _velocityBias;
        private Vector2 _worldAnchor1;
        private Vector2 _worldAnchor2;
        private Vector2 _worldAnchorDifferenceNormalized;
        private Body _body1;
        private Body _body2;

        public SliderJoint()
        {
        }

        public SliderJoint(Body body1, Vector2 anchor1, Body body2, Vector2 anchor2, float min, float max)
        {
            _body1 = body1;
            _body2 = body2;

            _anchor1 = anchor1;
            _anchor2 = anchor2;

            _min = min;
            _max = max;

            //initialize the world anchors (only needed to give valid values to the WorldAnchor properties)
            Anchor1 = anchor1;
            Anchor2 = anchor2;
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

        public float Slop
        {
            get { return _slop; }
            set { _slop = value; }
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

        public float Min
        {
            get { return _min; }
            set { _min = value; }
        }

        public float Max
        {
            get { return _max; }
            set { _max = value; }
        }

        public Vector2 Anchor1
        {
            get { return _anchor1; }
            set
            {
                _anchor1 = value;
                _body1.GetBodyMatrix(out _body1MatrixTemp);
                Vector2.TransformNormal(ref _anchor1, ref _body1MatrixTemp, out _r1);
                Vector2.Add(ref _body1.position, ref _r1, out _worldAnchor1);
            }
        }

        public Vector2 Anchor2
        {
            get { return _anchor2; }
            set
            {
                _anchor2 = value;
                _body2.GetBodyMatrix(out _body2MatrixTemp);
                Vector2.TransformNormal(ref _anchor2, ref _body2MatrixTemp, out _r2);
                Vector2.Add(ref _body2.position, ref _r2, out _worldAnchor2);
            }
        }

        public Vector2 WorldAnchor1
        {
            get { return _worldAnchor1; }
        }

        public Vector2 WorldAnchor2
        {
            get { return _worldAnchor2; }
        }

        public Vector2 CurrentAnchorPosition
        {
            get
            {
                Vector2.Add(ref _body1.position, ref _r1, out _anchor); //_anchor moves once simulator starts
                return _anchor;
            }
        }

        public event EventHandler<EventArgs> Broke;

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

            if (IsDisposed)
            {
                return;
            }

            //calc _r1 and _r2 from the anchors
            _body1.GetBodyMatrix(out _body1MatrixTemp);
            _body2.GetBodyMatrix(out _body2MatrixTemp);
            Vector2.TransformNormal(ref _anchor1, ref _body1MatrixTemp, out _r1);
            Vector2.TransformNormal(ref _anchor2, ref _body2MatrixTemp, out _r2);

            //calc the diff between _anchor positions
            Vector2.Add(ref _body1.position, ref _r1, out _worldAnchor1);
            Vector2.Add(ref _body2.position, ref _r2, out _worldAnchor2);
            Vector2.Subtract(ref _worldAnchor2, ref _worldAnchor1, out _worldAnchorDifference);

            _distance = _worldAnchorDifference.Length();
            _jointError = 0;

            if (_distance > _max)
            {
                if (_lowerLimitViolated)
                {
                    _accumulatedImpulse = 0;
                    _lowerLimitViolated = false;
                }
                _upperLimitViolated = true;
                if (_distance < _max + _slop)
                {
                    _jointError = 0; //allow some _slop 
                }
                else
                {
                    _jointError = _distance - _max;
                }
            }
            else if (_distance < _min)
            {
                if (_upperLimitViolated)
                {
                    _accumulatedImpulse = 0;
                    _upperLimitViolated = false;
                }
                _lowerLimitViolated = true;
                if (_distance > _min - _slop)
                {
                    _jointError = 0;
                }
                else
                {
                    _jointError = _distance - _min;
                }
            }
            else
            {
                _upperLimitViolated = false;
                _lowerLimitViolated = false;
                _jointError = 0;
                _accumulatedImpulse = 0;
            }

            //normalize the difference vector
            Vector2.Multiply(ref _worldAnchorDifference, 1/(_distance != 0 ? _distance : float.PositiveInfinity),
                             out _worldAnchorDifferenceNormalized); //_distance = 0 --> error (fix) 

            //calc velocity bias
            _velocityBias = _biasFactor*inverseDt*(_jointError);

            //calc mass normal (effective mass in relation to constraint)
            Calculator.Cross(ref _r1, ref _worldAnchorDifferenceNormalized, out _r1cn);
            Calculator.Cross(ref _r2, ref _worldAnchorDifferenceNormalized, out _r2cn);
            _kNormal = _body1.inverseMass + _body2.inverseMass + _body1.inverseMomentOfInertia*_r1cn*_r1cn +
                       _body2.inverseMomentOfInertia*_r2cn*_r2cn;
            _effectiveMass = (1)/(_kNormal + _softness);

            //convert scalar accumulated _impulse to vector
            Vector2.Multiply(ref _worldAnchorDifferenceNormalized, _accumulatedImpulse, out _accumulatedImpulseVector);

            //apply accumulated impulses (warm starting)
            _body2.ApplyImmediateImpulse(ref _accumulatedImpulseVector);
            Calculator.Cross(ref _r2, ref _accumulatedImpulseVector, out _angularImpulse);
            _body2.ApplyAngularImpulse(_angularImpulse);

            Vector2.Multiply(ref _accumulatedImpulseVector, -1, out _accumulatedImpulseVector);
            _body1.ApplyImmediateImpulse(ref _accumulatedImpulseVector);
            Calculator.Cross(ref _r1, ref _accumulatedImpulseVector, out _angularImpulse);
            _body1.ApplyAngularImpulse(_angularImpulse);
        }

        public override void Update()
        {
            if (Math.Abs(_jointError) > _breakpoint)
            {
                Dispose();
            } //check if joint is broken
            if (IsDisposed)
            {
                return;
            }
            if (!_upperLimitViolated && !_lowerLimitViolated)
            {
                return;
            }

            //calc velocity _anchor points (angular component + linear)
            Calculator.Cross(ref _body1.angularVelocity, ref _r1, out _angularVelocityComponent1);
            Vector2.Add(ref _body1.linearVelocity, ref _angularVelocityComponent1, out _velocity1);

            Calculator.Cross(ref _body2.angularVelocity, ref _r2, out _angularVelocityComponent2);
            Vector2.Add(ref _body2.linearVelocity, ref _angularVelocityComponent2, out _velocity2);

            //calc velocity difference
            Vector2.Subtract(ref _velocity2, ref _velocity1, out _dv);

            //map the velocity difference into constraint space
            Vector2.Dot(ref _dv, ref _worldAnchorDifferenceNormalized, out _dvNormal);

            //calc the _impulse magnitude
            _impulseMagnitude = (-_velocityBias - _dvNormal - _softness*_accumulatedImpulse)*_effectiveMass;
            //_softness not implemented correctly yet

            float oldAccumulatedImpulse = _accumulatedImpulse;

            if (_upperLimitViolated)
            {
                _accumulatedImpulse = Math.Min(oldAccumulatedImpulse + _impulseMagnitude, 0);
            }
            else if (_lowerLimitViolated)
            {
                _accumulatedImpulse = Math.Max(oldAccumulatedImpulse + _impulseMagnitude, 0);
            }

            _impulseMagnitude = _accumulatedImpulse - oldAccumulatedImpulse;

            //convert scalar _impulse to vector
            Vector2.Multiply(ref _worldAnchorDifferenceNormalized, _impulseMagnitude, out _impulse);

            //apply _impulse
            _body2.ApplyImmediateImpulse(ref _impulse);
            Calculator.Cross(ref _r2, ref _impulse, out _angularImpulse);
            _body2.ApplyAngularImpulse(_angularImpulse);

            Vector2.Multiply(ref _impulse, -1, out _impulse);
            _body1.ApplyImmediateImpulse(ref _impulse);
            Calculator.Cross(ref _r1, ref _impulse, out _angularImpulse);
            _body1.ApplyAngularImpulse(_angularImpulse);
        }

        #region Update variables

        private Vector2 _angularVelocityComponent1;
        private Vector2 _angularVelocityComponent2;
        private Vector2 _dv;
        private float _dvNormal;
        private Vector2 _impulse;
        private float _impulseMagnitude;
        private Vector2 _velocity1;
        private Vector2 _velocity2;

        #endregion

        #region PreStep variables

        private Vector2 _accumulatedImpulseVector;
        private float _angularImpulse;
        private Matrix _body1MatrixTemp;

        private Matrix _body2MatrixTemp;
        private float _distance;
        private float _kNormal;

        private float _r1cn;
        private float _r2cn;
        private Vector2 _worldAnchorDifference;

        #endregion
    }
}