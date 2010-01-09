﻿/*
* Box2D.XNA port of Box2D:
* Copyright (c) 2009 Brandon Furtwangler, Nathan Furtwangler
*
* Original source Box2D:
* Copyright (c) 2006-2009 Erin Catto http://www.gphysics.com 
* 
* This software is provided 'as-is', without any express or implied 
* warranty.  In no event will the authors be held liable for any damages 
* arising from the use of this software. 
* Permission is granted to anyone to use this software for any purpose, 
* including commercial applications, and to alter it and redistribute it 
* freely, subject to the following restrictions: 
* 1. The origin of this software must not be misrepresented; you must not 
* claim that you wrote the original software. If you use this software 
* in a product, an acknowledgment in the product documentation would be 
* appreciated but is not required. 
* 2. Altered source versions must be plainly marked as such, and must not be 
* misrepresented as being the original software. 
* 3. This notice may not be removed or altered from any source distribution. 
*/

using System;
using System.Diagnostics;
using Microsoft.Xna.Framework;

namespace FarseerPhysics
{
    [Flags]
    public enum WorldFlags
    {
        NewFixture = (1 << 0),
        Locked = (1 << 1),
    }

    /// <summary>
    /// The world class manages all physics entities, dynamic simulation,
    /// and asynchronous queries. The world also contains efficient memory
    /// management facilities.
    /// </summary>
    public class World
    {
        /// <summary>
        /// Called whenever a Fixture is removed
        /// </summary>
        public FixtureRemovedDelegate FixtureRemoved;

        internal WorldFlags Flags;

        /// <summary>
        /// Called whenever a Joint is removed
        /// </summary>
        public JointRemovedDelegate JointRemoved;

        private float _invDt0;
        private Island _island = new Island();
        private Func<Fixture, bool> _queryAABBCallback;
        private Func<int, bool> _queryAABBCallbackWrapper;

        private WorldRayCastCallback _rayCastCallback;
        private RayCastCallback _rayCastCallbackWrapper;

        private Stopwatch _watch;

        /// <summary>
        /// Construct a world object.
        /// </summary>
        /// <param name="gravity">the world gravity vector.</param>
        /// <param name="allowSleep">improve performance by not simulating inactive bodies.</param>
        public World(Vector2 gravity, bool allowSleep)
        {
            WarmStarting = true;
            ContinuousPhysics = true;

            AllowSleep = allowSleep;
            Gravity = gravity;

            _queryAABBCallbackWrapper = QueryAABBCallbackWrapper;
            _rayCastCallbackWrapper = RayCastCallbackWrapper;

            ContactManager = new ContactManager();

            //Create the default contact filter
            new DefaultContactFilter(this);

            // init the watch
            _watch = new Stopwatch();
        }

        /// <summary>
        /// Enable/disable warm starting. For testing.
        /// </summary>
        /// <value><c>true</c> if [warm starting]; otherwise, <c>false</c>.</value>
        public bool WarmStarting { get; set; }

        /// <summary>
        /// Enable/disable continuous physics. For testing.
        /// </summary>
        /// <value><c>true</c> if [continuous physics]; otherwise, <c>false</c>.</value>
        public bool ContinuousPhysics { get; set; }

        /// <summary>
        /// Get the number of broad-phase proxies.
        /// </summary>
        /// <value>The proxy count.</value>
        public int ProxyCount
        {
            get { return ContactManager._broadPhase.ProxyCount; }
        }

        /// <summary>
        /// Get the number of bodies.
        /// </summary>
        /// <value>The body count.</value>
        public int BodyCount { get; private set; }

        /// <summary>
        /// Get the number of joints.
        /// </summary>
        /// <value>The joint count.</value>
        public int JointCount { get; private set; }

        /// <summary>
        /// Get the number of contacts (each may have 0 or more contact points).
        /// </summary>
        /// <value>The contact count.</value>
        public int ContactCount
        {
            get { return ContactManager._contactCount; }
        }

        /// <summary>
        /// Change the global gravity vector.
        /// </summary>
        /// <value>The gravity.</value>
        public Vector2 Gravity { get; set; }

        /// <summary>
        /// Is the world locked (in the middle of a time step).
        /// </summary>
        /// <value><c>true</c> if this instance is locked; otherwise, <c>false</c>.</value>
        public bool Locked
        {
            get { return (Flags & WorldFlags.Locked) == WorldFlags.Locked; }
            set
            {
                if (value)
                {
                    Flags |= WorldFlags.Locked;
                }
                else
                {
                    Flags &= ~WorldFlags.Locked;
                }
            }
        }

        /// <summary>
        /// Get the world body list. With the returned body, use Body.GetNext to get
        /// the next body in the world list. A null body indicates the end of the list.
        /// </summary>
        /// <returns>the head of the world body list.</returns>
        public Body BodyList { get; private set; }

        public ContactManager ContactManager { get; private set; }

        /// <summary>
        /// Get the world joint list. With the returned joint, use Joint.GetNext to get
        /// the next joint in the world list. A null joint indicates the end of the list.
        /// </summary>
        /// <returns>the head of the world joint list.</returns>
        public Joint JointList { get; private set; }

        public bool AllowSleep { get; private set; }

        public TimeSpan UpdateTime { get; private set; }

        /// <summary>
        /// Get the world contact list. With the returned contact, use Contact.GetNext to get
        /// the next contact in the world list. A null contact indicates the end of the list.
        /// </summary>
        /// <returns>the head of the world contact list.</returns>
        public Contact ContactList
        {
            get { return ContactManager._contactList; }
        }

        public Body CreateBody()
        {
            Debug.Assert(!Locked);
            if (Locked)
            {
                return null;
            }

            Body b = new Body(this);

            // Add to world doubly linked list.
            b._prev = null;
            b._next = BodyList;
            if (BodyList != null)
            {
                BodyList._prev = b;
            }
            BodyList = b;
            ++BodyCount;

            return b;
        }

        public Body CreateBody(Body body)
        {
            Debug.Assert(!Locked);
            if (Locked)
            {
                return null;
            }

            //NOTE: This constructor is untested and might not work.
            Body b = new Body(body, this);

            // Add to world doubly linked list.
            b._prev = null;
            b._next = BodyList;
            if (BodyList != null)
            {
                BodyList._prev = b;
            }
            BodyList = b;
            ++BodyCount;

            return b;
        }

        /// <summary>
        /// Destroy a rigid body given a definition. No reference to the definition
        /// is retained. This function is locked during callbacks.
        /// @warning This automatically deletes all associated shapes and joints.
        /// @warning This function is locked during callbacks.
        /// </summary>
        /// <param name="b">The b.</param>
        public void DestroyBody(Body b)
        {
            Debug.Assert(BodyCount > 0);
            Debug.Assert(!Locked);
            if (Locked)
            {
                return;
            }

            // Delete the attached joints.
            JointEdge je = b._jointList;
            while (je != null)
            {
                JointEdge je0 = je;
                je = je.Next;

                if (JointRemoved != null)
                {
                    JointRemoved(je0.Joint);
                }

                DestroyJoint(je0.Joint);
            }
            b._jointList = null;

            // Delete the attached contacts.
            ContactEdge ce = b._contactList;
            while (ce != null)
            {
                ContactEdge ce0 = ce;
                ce = ce.Next;
                ContactManager.Destroy(ce0.Contact);
            }
            b._contactList = null;

            // Delete the attached fixtures. This destroys broad-phase proxies.
            Fixture f = b._fixtureList;
            while (f != null)
            {
                Fixture f0 = f;
                f = f._next;

                if (FixtureRemoved != null)
                {
                    FixtureRemoved(f0);
                }

                f0.DestroyProxy(ContactManager._broadPhase);
                f0.Destroy();
            }
            b._fixtureList = null;
            b._fixtureCount = 0;

            // Remove world body list.
            if (b._prev != null)
            {
                b._prev._next = b._next;
            }

            if (b._next != null)
            {
                b._next._prev = b._prev;
            }

            if (b == BodyList)
            {
                BodyList = b._next;
            }

            --BodyCount;
        }

        /// <summary>
        /// Create a joint to rain bodies together. No reference to the definition
        /// is retained. This may cause the connected bodies to cease colliding.
        /// @warning This function is locked during callbacks.
        /// </summary>
        /// <param name="joint">The joint.</param>
        /// <returns></returns>
        public void CreateJoint(Joint joint)
        {
            Debug.Assert(!Locked);
            if (Locked)
            {
                return;
            }

            // Connect to the world list.
            joint.Prev = null;
            joint.Next = JointList;
            if (JointList != null)
            {
                JointList.Prev = joint;
            }
            JointList = joint;
            ++JointCount;

            // Connect to the bodies' doubly linked lists.
            joint._edgeA.Joint = joint;
            joint._edgeA.Other = joint.BodyB;
            joint._edgeA.Prev = null;
            joint._edgeA.Next = joint.BodyA._jointList;

            if (joint.BodyA._jointList != null)
                joint.BodyA._jointList.Prev = joint._edgeA;

            joint.BodyA._jointList = joint._edgeA;

            joint._edgeB.Joint = joint;
            joint._edgeB.Other = joint.BodyA;
            joint._edgeB.Prev = null;
            joint._edgeB.Next = joint.BodyB._jointList;

            if (joint.BodyB._jointList != null)
                joint.BodyB._jointList.Prev = joint._edgeB;

            joint.BodyB._jointList = joint._edgeB;

            Body bodyA = joint.BodyA;
            Body bodyB = joint.BodyB;

            // If the joint prevents collisions, then flag any contacts for filtering.
            if (joint.CollideConnected == false)
            {
                ContactEdge edge = bodyB.ContactList;
                while (edge != null)
                {
                    if (edge.Other == bodyA)
                    {
                        // Flag the contact for filtering at the next time step (where either
                        // body is awake).
                        edge.Contact.FlagForFiltering();
                    }

                    edge = edge.Next;
                }
            }

            // Note: creating a joint doesn't wake the bodies.
        }

        /// <summary>
        /// Destroy a joint. This may cause the connected bodies to begin colliding.
        /// @warning This function is locked during callbacks.
        /// </summary>
        /// <param name="j">The j.</param>
        public void DestroyJoint(Joint j)
        {
            Debug.Assert(!Locked);
            if (Locked)
            {
                return;
            }

            bool collideConnected = j.CollideConnected;

            // Remove from the doubly linked list.
            if (j.Prev != null)
            {
                j.Prev.Next = j.Next;
            }

            if (j.Next != null)
            {
                j.Next.Prev = j.Prev;
            }

            if (j == JointList)
            {
                JointList = j.Next;
            }

            // Disconnect from island graph.
            Body bodyA = j.BodyA;
            Body bodyB = j.BodyB;

            // Wake up connected bodies.
            bodyA.Awake = true;
            bodyB.Awake = true;

            // Remove from body 1.
            if (j._edgeA.Prev != null)
            {
                j._edgeA.Prev.Next = j._edgeA.Next;
            }

            if (j._edgeA.Next != null)
            {
                j._edgeA.Next.Prev = j._edgeA.Prev;
            }

            if (j._edgeA == bodyA._jointList)
            {
                bodyA._jointList = j._edgeA.Next;
            }

            j._edgeA.Prev = null;
            j._edgeA.Next = null;

            // Remove from body 2
            if (j._edgeB.Prev != null)
            {
                j._edgeB.Prev.Next = j._edgeB.Next;
            }

            if (j._edgeB.Next != null)
            {
                j._edgeB.Next.Prev = j._edgeB.Prev;
            }

            if (j._edgeB == bodyB._jointList)
            {
                bodyB._jointList = j._edgeB.Next;
            }

            j._edgeB.Prev = null;
            j._edgeB.Next = null;

            Debug.Assert(JointCount > 0);
            --JointCount;

            // If the joint prevents collisions, then flag any contacts for filtering.
            if (collideConnected == false)
            {
                ContactEdge edge = bodyB.ContactList;
                while (edge != null)
                {
                    if (edge.Other == bodyA)
                    {
                        // Flag the contact for filtering at the next time step (where either
                        // body is awake).
                        edge.Contact.FlagForFiltering();
                    }

                    edge = edge.Next;
                }
            }
        }

        /// <summary>
        /// Take a time step. This performs collision detection, integration,
        /// and constraint solution.
        /// </summary>
        /// <param name="dt">the amount of time to simulate, this should not vary.</param>
        /// <param name="velocityIterations">for the velocity constraint solver.</param>
        /// <param name="positionIterations">for the position constraint solver.</param>
        public void Step(float dt, int velocityIterations, int positionIterations)
        {
            _watch.Start();
            
            // If new fixtures were added, we need to find the new contacts.
            if ((Flags & WorldFlags.NewFixture) == WorldFlags.NewFixture)
            {
                ContactManager.FindNewContacts();
                Flags &= ~WorldFlags.NewFixture;
            }

            //Lock the world
            Flags |= WorldFlags.Locked;

            TimeStep step;
            step.DeltaTime = dt;
            step.VelocityIterations = velocityIterations;
            step.PositionIterations = positionIterations;
            if (dt > 0.0f)
            {
                step.Inv_DeltaTime = 1.0f / dt;
            }
            else
            {
                step.Inv_DeltaTime = 0.0f;
            }

            step.DtRatio = _invDt0 * dt;

            step.WarmStarting = WarmStarting;

            // Update contacts. This is where some contacts are destroyed.
            ContactManager.Collide();

            // Integrate velocities, solve velocity constraints, and integrate positions.
            if (step.DeltaTime > 0.0f)
            {
                Solve(ref step);
            }

            // Handle TOI events.
            if (ContinuousPhysics && step.DeltaTime > 0.0f)
            {
                SolveTOI(ref step);
            }

            if (step.DeltaTime > 0.0f)
            {
                _invDt0 = step.Inv_DeltaTime;
            }

            Flags &= ~WorldFlags.Locked;

            _watch.Stop();
            UpdateTime = _watch.Elapsed;
            _watch.Reset();
        }

        /// <summary>
        /// Call this after you are done with time steps to clear the forces. You normally
        /// call this after each call to Step, unless you are performing sub-steps.
        /// </summary>
        public void ClearForces()
        {
            for (Body body = BodyList; body != null; body = body.NextBody)
            {
                body._force = Vector2.Zero;
                body._torque = 0.0f;
            }
        }

        /// <summary>
        /// Query the world for all fixtures that potentially overlap the
        /// provided AABB.
        /// </summary>
        /// <param name="callback">a user implemented callback class.</param>
        /// <param name="aabb">the query box.</param>
        public void QueryAABB(Func<Fixture, bool> callback, ref AABB aabb)
        {
            _queryAABBCallback = callback;
            ContactManager._broadPhase.Query(_queryAABBCallbackWrapper, ref aabb);
            _queryAABBCallback = null;
        }

        private bool QueryAABBCallbackWrapper(int proxyId)
        {
            Fixture fixture = ContactManager._broadPhase.GetUserData<Fixture>(proxyId);
            return _queryAABBCallback(fixture);
        }

        /// <summary>
        /// Ray-cast the world for all fixtures in the path of the ray. Your callback
        /// controls whether you get the closest point, any point, or n-points.
        /// The ray-cast ignores shapes that contain the starting point.
        /// </summary>
        /// <param name="callback">a user implemented callback class.</param>
        /// <param name="point1">the ray starting point</param>
        /// <param name="point2">the ray ending point</param>
        public void RayCast(WorldRayCastCallback callback, Vector2 point1, Vector2 point2)
        {
            RayCastInput input = new RayCastInput();
            input.MaxFraction = 1.0f;
            input.Point1 = point1;
            input.Point2 = point2;

            _rayCastCallback = callback;
            ContactManager._broadPhase.RayCast(_rayCastCallbackWrapper, ref input);
            _rayCastCallback = null;
        }

        private float RayCastCallbackWrapper(ref RayCastInput input, int proxyId)
        {
            Fixture fixture = ContactManager._broadPhase.GetUserData<Fixture>(proxyId);
            RayCastOutput output;
            bool hit = fixture.RayCast(out output, ref input);

            if (hit)
            {
                float fraction = output.Fraction;
                Vector2 point = (1.0f - fraction) * input.Point1 + fraction * input.Point2;
                return _rayCastCallback(fixture, point, output.Normal, fraction);
            }

            return input.MaxFraction;
        }

        private Body[] _stack;

        private void Solve(ref TimeStep step)
        {
            // Size the island for the worst case.
            _island.Reset(BodyCount,
                          ContactManager._contactCount,
                          JointCount,
                          ContactManager);

            // Clear all the island flags.
            for (Body b = BodyList; b != null; b = b._next)
            {
                b._flags &= ~BodyFlags.Island;
            }
            for (Contact c = ContactManager._contactList; c != null; c = c.NextContact)
            {
                c.Flags &= ~ContactFlags.Island;
            }
            for (Joint j = JointList; j != null; j = j.Next)
            {
                j._islandFlag = false;
            }

            // Build and simulate all awake islands.
            int stackSize = BodyCount;

            if (_stack == null || _stack.Length < stackSize)
                _stack = new Body[BodyCount];

            for (Body seed = BodyList; seed != null; seed = seed._next)
            {
                if ((seed._flags & (BodyFlags.Island)) != BodyFlags.None)
                {
                    continue;
                }

                if (seed.Awake == false || seed.Active == false)
                {
                    continue;
                }

                // The seed can be dynamic or kinematic.
                if (seed.BodyType == BodyType.Static)
                {
                    continue;
                }

                // Reset island and stack.
                _island.Clear();
                int stackCount = 0;
                _stack[stackCount++] = seed;
                seed._flags |= BodyFlags.Island;

                // Perform a depth first search (DFS) on the constraint graph.
                while (stackCount > 0)
                {
                    // Grab the next body off the stack and add it to the island.
                    Body b = _stack[--stackCount];
                    Debug.Assert(b.Active);
                    _island.Add(b);

                    // Make sure the body is awake.
                    if (b.Awake == false)
                    {
                        b.Awake = true;
                    }

                    // To keep islands as small as possible, we don't
                    // propagate islands across static bodies.
                    if (b.BodyType == BodyType.Static)
                    {
                        continue;
                    }

                    // Search all contacts connected to this body.
                    for (ContactEdge ce = b._contactList; ce != null; ce = ce.Next)
                    {
                        // Has this contact already been added to an island?
                        if ((ce.Contact.Flags & ContactFlags.Island) != ContactFlags.None)
                        {
                            continue;
                        }

                        // Is this contact solid and touching?
                        if (ce.Contact.Sensor || !ce.Contact.Enabled || !ce.Contact.Touching)
                        {
                            continue;
                        }

                        _island.Add(ce.Contact);
                        ce.Contact.Flags |= ContactFlags.Island;

                        Body other = ce.Other;

                        // Was the other body already added to this island?
                        if ((other._flags & BodyFlags.Island) != BodyFlags.None)
                        {
                            continue;
                        }

                        Debug.Assert(stackCount < stackSize);
                        _stack[stackCount++] = other;
                        other._flags |= BodyFlags.Island;
                    }

                    // Search all joints connect to this body.
                    for (JointEdge je = b._jointList; je != null; je = je.Next)
                    {
                        if (je.Joint._islandFlag)
                        {
                            continue;
                        }

                        Body other = je.Other;

                        // Don't simulate joints connected to inactive bodies.
                        if (other.Active == false)
                        {
                            continue;
                        }

                        _island.Add(je.Joint);
                        je.Joint._islandFlag = true;

                        if ((other._flags & BodyFlags.Island) != BodyFlags.None)
                        {
                            continue;
                        }

                        Debug.Assert(stackCount < stackSize);
                        _stack[stackCount++] = other;
                        other._flags |= BodyFlags.Island;
                    }
                }

                _island.Solve(ref step, Gravity, AllowSleep);

                // Post solve cleanup.
                for (int i = 0; i < _island.BodyCount; ++i)
                {
                    // Allow static bodies to participate in other islands.
                    Body b = _island.Bodies[i];
                    if (b.BodyType == BodyType.Static)
                    {
                        b._flags &= ~BodyFlags.Island;
                    }
                }
            }

            // Synchronize fixtures, check for out of range bodies.
            for (Body b = BodyList; b != null; b = b.NextBody)
            {
                if (!b.Awake || !b.Active)
                {
                    continue;
                }

                if (b.BodyType == BodyType.Static)
                {
                    continue;
                }

                // Update fixtures (for broad-phase).
                b.SynchronizeFixtures();
            }

            // Look for new contacts.
            ContactManager.FindNewContacts();
        }

        private Body[] _queue;

        private void SolveTOI(ref TimeStep step)
        {
            // Reserve an island and a queue for TOI island solution.
            _island.Reset(BodyCount,
                          Settings.MaxTOIContactsPerIsland,
                          Settings.MaxTOIJointsPerIsland,
                          ContactManager);

            //Simple one pass queue
            //Relies on the fact that we're only making one pass
            //through and each body can only be pushed/popped once.
            //To push: 
            //  queue[queueStart+queueSize++] = newElement;
            //To pop: 
            //	poppedElement = queue[queueStart++];
            //  --queueSize;

            int queueCapacity = BodyCount;

            if (_queue == null || _queue.Length < queueCapacity)
                _queue = new Body[BodyCount];

            for (Body b = BodyList; b != null; b = b._next)
            {
                b._flags &= ~BodyFlags.Island;
                b._sweep.TimeInt0 = 0.0f;
            }

            for (Contact c = ContactManager._contactList; c != null; c = c.NextContact)
            {
                // Invalidate TOI
                c.Flags &= ~(ContactFlags.Toi | ContactFlags.Island);
            }

            for (Joint j = JointList; j != null; j = j.Next)
            {
                j._islandFlag = false;
            }

            // Find TOI events and solve them.
            for (; ; )
            {
                // Find the first TOI.
                Contact minContact = null;
                float minTOI = 1.0f;

                for (Contact c = ContactManager._contactList; c != null; c = c.NextContact)
                {
                    // Can this contact generate a solid TOI contact?
                    if (c.Sensor || c.Enabled == false || c.Continuous == false)
                    {
                        continue;
                    }

                    // TODO_ERIN keep a counter on the contact, only respond to M TOIs per contact.

                    float toi;
                    if ((c.Flags & ContactFlags.Toi) != ContactFlags.None)
                    {
                        // This contact has a valid cached TOI.
                        toi = c.TOI;
                    }
                    else
                    {
                        // Compute the TOI for this contact.
                        Fixture s1 = c.FixtureA;
                        Fixture s2 = c.FixtureB;
                        Body b1 = s1.Body;
                        Body b2 = s2.Body;

                        if ((b1.BodyType != BodyType.Dynamic || !b1.Awake) &&
                            (b2.BodyType != BodyType.Dynamic || !b2.Awake))
                        {
                            continue;
                        }

                        // Put the sweeps onto the same time interval.
                        float t0 = b1._sweep.TimeInt0;

                        if (b1._sweep.TimeInt0 < b2._sweep.TimeInt0)
                        {
                            t0 = b2._sweep.TimeInt0;
                            b1._sweep.Advance(t0);
                        }
                        else if (b2._sweep.TimeInt0 < b1._sweep.TimeInt0)
                        {
                            t0 = b1._sweep.TimeInt0;
                            b2._sweep.Advance(t0);
                        }

                        Debug.Assert(t0 < 1.0f);

                        // Compute the time of impact.
                        toi = c.ComputeTOI(ref b1._sweep, ref b2._sweep);

                        Debug.Assert(0.0f <= toi && toi <= 1.0f);

                        // If the TOI is in range ...
                        if (0.0f < toi && toi < 1.0f)
                        {
                            // Interpolate on the actual range.
                            toi = Math.Min((1.0f - toi) * t0 + toi, 1.0f);
                        }


                        c.TOI = toi;
                        c.Flags |= ContactFlags.Toi;
                    }

                    if (Settings.Epsilon < toi && toi < minTOI)
                    {
                        // This is the minimum TOI found so far.
                        minContact = c;
                        minTOI = toi;
                    }
                }

                if (minContact == null || 1.0f - 100.0f * Settings.Epsilon < minTOI)
                {
                    // No more TOI events. Done!
                    break;
                }

                // Advance the bodies to the TOI.
                Fixture s1_2 = minContact.FixtureA;
                Fixture s2_2 = minContact.FixtureB;
                Body b1_2 = s1_2.Body;
                Body b2_2 = s2_2.Body;

                Sweep backup1 = b1_2._sweep;
                Sweep backup2 = b2_2._sweep;

                b1_2.Advance(minTOI);
                b2_2.Advance(minTOI);

                // The TOI contact likely has some new contact points.
                minContact.Update(ContactManager);
                minContact.Flags &= ~ContactFlags.Toi;

                // Is the contact solid?
                if (minContact.Sensor || !minContact.Enabled)
                {
                    // Restore the sweeps.
                    b1_2._sweep = backup1;
                    b2_2._sweep = backup2;
                    b1_2.SynchronizeTransform();
                    b2_2.SynchronizeTransform();
                    continue;
                }

                // Did numerical issues prevent a contact point from being generated?
                if (!minContact.Touching)
                {
                    // Give up on this TOI.
                    continue;
                }

                // Build the TOI island. We need a dynamic seed.
                Body seed = b1_2;
                if (seed.BodyType != BodyType.Dynamic)
                {
                    seed = b2_2;
                }

                // Reset island and queue.
                _island.Clear();

                int queueStart = 0; // starting index for queue
                int queueSize = 0; // elements in queue
                _queue[queueStart + queueSize++] = seed;
                seed._flags |= BodyFlags.Island;

                // Perform a breadth first search (BFS) on the contact/joint graph.
                while (queueSize > 0)
                {
                    // Grab the next body off the stack and add it to the island.
                    Body b = _queue[queueStart++];
                    --queueSize;

                    _island.Add(b);

                    // Make sure the body is awake.
                    if (b.Awake == false)
                    {
                        b.Awake = true;
                    }


                    // To keep islands as small as possible, we don't
                    // propagate islands across static or kinematic bodies.
                    if (b.BodyType != BodyType.Dynamic)
                    {
                        continue;
                    }

                    // Search all contacts connected to this body.
                    for (ContactEdge cEdge = b._contactList; cEdge != null; cEdge = cEdge.Next)
                    {
                        // Does the TOI island still have space for contacts?
                        if (_island.ContactCount == _island.ContactCapacity)
                        {
                            break;
                        }

                        // Has this contact already been added to an island?
                        if ((cEdge.Contact.Flags & ContactFlags.Island) != ContactFlags.None)
                        {
                            continue;
                        }

                        // Skip separate, sensor, or disabled contacts.
                        if (cEdge.Contact.Sensor ||
                            cEdge.Contact.Enabled == false ||
                            cEdge.Contact.Touching == false)
                        {
                            continue;
                        }

                        _island.Add(cEdge.Contact);
                        cEdge.Contact.Flags |= ContactFlags.Island;

                        // Update other body.
                        Body other = cEdge.Other;

                        // Was the other body already added to this island?
                        if ((other._flags & BodyFlags.Island) != BodyFlags.None)
                        {
                            continue;
                        }

                        // Synchronize the connected body.
                        if (other.BodyType != BodyType.Static)
                        {
                            other.Advance(minTOI);
                            other.Awake = true;
                        }

                        Debug.Assert(queueStart + queueSize < queueCapacity);
                        _queue[queueStart + queueSize] = other;
                        ++queueSize;
                        other._flags |= BodyFlags.Island;
                    }

                    for (JointEdge jEdge = b._jointList; jEdge != null; jEdge = jEdge.Next)
                    {
                        if (_island.JointCount == _island.JointCapacity)
                        {
                            continue;
                        }

                        if (jEdge.Joint._islandFlag)
                        {
                            continue;
                        }

                        Body other = jEdge.Other;
                        if (other.Active == false)
                        {
                            continue;
                        }

                        _island.Add(jEdge.Joint);

                        jEdge.Joint._islandFlag = true;

                        if ((other._flags & BodyFlags.Island) != BodyFlags.None)
                        {
                            continue;
                        }

                        // Synchronize the connected body.
                        if (other.BodyType != BodyType.Static)
                        {
                            other.Advance(minTOI);
                            other.Awake = true;
                        }

                        Debug.Assert(queueStart + queueSize < queueCapacity);
                        _queue[queueStart + queueSize] = other;
                        ++queueSize;
                        other._flags |= BodyFlags.Island;
                    }
                }

                TimeStep subStep;
                subStep.WarmStarting = false;
                subStep.DeltaTime = (1.0f - minTOI) * step.DeltaTime;
                subStep.Inv_DeltaTime = 1.0f / subStep.DeltaTime;
                subStep.DtRatio = 0.0f;
                subStep.VelocityIterations = step.VelocityIterations;
                subStep.PositionIterations = step.PositionIterations;

                _island.SolveTOI(ref subStep);

                // Post solve cleanup.
                for (int i = 0; i < _island.BodyCount; ++i)
                {
                    // Allow bodies to participate in future TOI islands.
                    Body b = _island.Bodies[i];
                    b._flags &= ~BodyFlags.Island;

                    if (b.Awake == false)
                    {
                        continue;
                    }

                    if (b.BodyType == BodyType.Static)
                    {
                        continue;
                    }

                    // Update fixtures (for broad-phase).
                    b.SynchronizeFixtures();

                    // Invalidate all contact TOIs associated with this body. Some of these
                    // may not be in the island because they were not touching.
                    for (ContactEdge ce = b._contactList; ce != null; ce = ce.Next)
                    {
                        ce.Contact.Flags &= ~ContactFlags.Toi;
                    }
                }

                int contactCount = _island.ContactCount;
                for (int i = 0; i < contactCount; ++i)
                {
                    // Allow contacts to participate in future TOI islands.
                    Contact c = _island.Contacts[i];
                    c.Flags &= ~(ContactFlags.Toi | ContactFlags.Island);
                }

                for (int i = 0; i < _island.JointCount; ++i)
                {
                    // Allow joints to participate in future TOI islands.
                    Joint j = _island.Joints[i];
                    j._islandFlag = false;
                }

                // Commit fixture proxy movements to the broad-phase so that new contacts are created.
                // Also, some contacts can be destroyed.
                ContactManager.FindNewContacts();
            }
        }
    }
}