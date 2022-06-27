namespace QuizCanners.TinyECS
{
    public delegate void SystemActionI<T>(T item);
    public delegate void SystemActionR<T>(ref T item);
    public delegate void SystemActionRE<T>(ref T item, IEntity entity);

    public delegate void SystemActionRR<T1, T2>(ref T1 item1, ref T2 item2);
    public delegate void SystemActionRI<T1, T2>(ref T1 item1, T2 item2);
    public delegate void SystemActionRIE<T1, T2>(ref T1 item1, T2 item2, IEntity entity);
    public delegate void SystemActionII<T1, T2>(T1 item1, T2 item2);
    public delegate void SystemActionIIE<T1, T2>(T1 item1, T2 item2, IEntity entity);
    public delegate void SystemActionRRE<T1, T2>(ref T1 item1, ref T2 item2, IEntity entity);

    public delegate void SystemActionIII<T1, T2, T3>(T1 item1, T2 item2, T3 item3);
    public delegate void SystemActionRII<T1, T2, T3>(ref T1 item1, T2 item2, T3 item3);
    public delegate void SystemActionRRI<T1, T2, T3>(ref T1 item1, ref T2 item2, T3 item3);
    public delegate void SystemActionRRR<T1, T2, T3>(ref T1 item1, ref T2 item2, ref T3 item3);

    public delegate void SystemActionIIIE<T1, T2, T3>(T1 item1, T2 item2, T3 item3, IEntity entity);
    public delegate void SystemActionRIIE<T1, T2, T3>(ref T1 item1, T2 item2, T3 item3, IEntity entity);

    /*
    public interface ISelection
    {
        void AddFilter<C>() where C : struct;
    }

    
    public static class SelectionRunSystemExtensions 
    {
        public static S WithComponent<S,C>(this S selection) where S:ISelection where C: struct 
        {
            selection.AddFilter<C>();
            return selection;
        }

        public static S WithComponent<S, C1, C2>(this S selection) where S : ISelection where C1 : struct where C2 : struct
        {
            selection.AddFilter<C1>();
            selection.AddFilter<C2>();
            return selection;
        }
    }*/

    public partial class World<W>
    {
        public Selection<T> WithAll<T>() where T : struct => new(world: this);
        public Selection<T1, T2> WithAll<T1, T2>() where T1 : struct where T2 : struct => new(world: this);
        public Selection<T1, T2, T3> WithAll<T1, T2, T3>() where T1 : struct where T2 : struct where T3 : struct => new(world: this);


        public struct Selection<T> where T : struct
        {
            internal readonly World<W> _world;
            internal readonly ComponentArrayGenric<T>.ComponentArray _components;

            public int Flag1 { get; private set; }
            public int CombinedFlag { get; private set; }

            public Selection<T> AddFilter<C>() where C:struct 
            {
                CombinedFlag |= _world.GetFlag<C>();
                return this;
            }

            internal Selection(World<W> world) 
            {
                _world = world;
                Flag1 = _world.GetFlag<T>();
                CombinedFlag = Flag1;
               

                if (!_world.allComponents.TryGetValue(Flag1, out var cmps))
                    _components = null;
                else 
                    _components = (cmps as ComponentArrayGenric<T>).components;
            }

            public void Run(SystemActionI<T> action) 
            {
                if (_components == null)
                    return;

                foreach (Entity e in _world.allEntities)
                {
                    EntityComponentsList componentsList = _world[e];

                    if (!componentsList.HasComponents(CombinedFlag))
                        continue;

                    var index = componentsList[Flag1].Index;

                    action.Invoke(_components[index]);
                }
            }

            public void Run(SystemActionR<T> action)
            {
                if (_components == null)
                    return;

                foreach (Entity e in _world.allEntities)
                {
                    EntityComponentsList componentsList = _world[e];

                    if (!componentsList.HasComponents(CombinedFlag))
                        continue;

                    var index = componentsList[Flag1].Index;

                    var el = _components[index];

                    action.Invoke(ref el);

                    _components[index] = el;
                }
            }

            public void Run(SystemActionRE<T> action)
            {
                if (_components == null)
                    return;

                foreach (Entity e in _world.allEntities)
                {
                    EntityComponentsList componentsList = _world[e];

                    if (!componentsList.HasComponents(CombinedFlag))
                        continue;

                    var index = componentsList[Flag1].Index;

                    var el = _components[index];

                    action.Invoke(ref el, e);

                    _components[index] = el;
                }
            }
        }

        public struct Selection<T1, T2> where T1 : struct where T2 : struct
        {
            readonly bool _setup;
            readonly int _flag1;
            readonly int _flag2;
            public int CombinedFlag { get; private set; }
            readonly World<W> _world;
            readonly ComponentArrayGenric<T1>.ComponentArray _components1;
            readonly ComponentArrayGenric<T2>.ComponentArray _components2;

            public Selection<T1, T2> AddFilter<C>() where C : struct
            {
                CombinedFlag |= _world.GetFlag<C>();
                return this;
            }

            internal Selection(World<W> world)
            {
                _flag1 = world.GetFlag<T1>();
                _flag2 = world.GetFlag<T2>();

                CombinedFlag = _flag1 | _flag2;
                _world = world;
                _setup = true;

                if (!_world.allComponents.TryGetValue(_flag1, out var cmps))
                {
                    _setup = false;
                    _components1 = null;
                }
                else
                    _components1 = (cmps as ComponentArrayGenric<T1>).components;

                if (!_world.allComponents.TryGetValue(_flag2, out var cmps2))
                {
                    _setup = false;
                    _components2 = null;
                }
                else
                    _components2 = (cmps2 as ComponentArrayGenric<T2>).components;
            }

            public void Run(SystemActionRI<T1, T2> action)
            {
                if (!_setup)
                    return;

                foreach (Entity e in _world.allEntities)
                {
                    EntityComponentsList componentsList = _world[e];

                    if (!componentsList.HasComponents(CombinedFlag))
                        continue;

                    var index1 = componentsList[_flag1].Index;
                    var index2 = componentsList[_flag2].Index;

                    var el1 = _components1[index1];
                    action.Invoke(ref el1, _components2[index2]);
                    _components1[index1] = el1;
                }
            }

            public void Run(SystemActionRR<T1, T2> action)
            {
                if (!_setup)
                    return;

                foreach (Entity e in _world.allEntities)
                {
                    EntityComponentsList componentsList = _world[e];

                    if (!componentsList.HasComponents(CombinedFlag))
                        continue;

                    var index1 = componentsList[_flag1].Index;
                    var index2 = componentsList[_flag2].Index;

                    var el1 = _components1[index1];
                    var el2 = _components2[index2];
                    action.Invoke(ref el1, ref el2);
                    _components1[index1] = el1;
                    _components2[index2] = el2;
                }
            }

            public void Run(SystemActionII<T1, T2> action)
            {
                if (!_setup)
                    return;

                foreach (Entity e in _world.allEntities)
                {
                    EntityComponentsList componentsList = _world[e];

                    if (!componentsList.HasComponents(CombinedFlag))
                        continue;

                    var index1 = componentsList[_flag1].Index;
                    var index2 = componentsList[_flag2].Index;

                    action.Invoke(_components1[index1], _components2[index2]);
                }
            }

            public void Run(SystemActionRIE<T1, T2> action)
            {
                if (!_setup)
                    return;

                foreach (Entity e in _world.allEntities)
                {
                    EntityComponentsList componentsList = _world[e];

                    if (!componentsList.HasComponents(CombinedFlag))
                        continue;

                    var index1 = componentsList[_flag1].Index;
                    var index2 = componentsList[_flag2].Index;

                    var el1 = _components1[index1];
                    action.Invoke(ref el1, _components2[index2], e);
                    _components1[index1] = el1;
                }
            }

            public void Run(SystemActionRRE<T1, T2> action)
            {
                if (!_setup)
                    return;

                foreach (Entity e in _world.allEntities)
                {
                    EntityComponentsList componentsList = _world[e];

                    if (!componentsList.HasComponents(CombinedFlag))
                        continue;

                    var index1 = componentsList[_flag1].Index;
                    var index2 = componentsList[_flag2].Index;

                    var el1 = _components1[index1];
                    var el2 = _components2[index2];
                    action.Invoke(ref el1, ref el2, e);
                    _components1[index1] = el1;
                    _components2[index2] = el2;
                }
            }

            public void Run(SystemActionIIE<T1, T2> action)
            {
                if (!_setup)
                    return;

                foreach (Entity e in _world.allEntities)
                {
                    EntityComponentsList componentsList = _world[e];

                    if (!componentsList.HasComponents(CombinedFlag))
                        continue;

                    var index1 = componentsList[_flag1].Index;
                    var index2 = componentsList[_flag2].Index;

                    action.Invoke(_components1[index1], _components2[index2], e);
                }
            }
        }

        public struct Selection<T1, T2, T3>  where T1:struct where T2 : struct where T3 : struct
        {
            readonly bool _setup;
            readonly int _flag1;
            readonly int _flag2;
            readonly int _flag3;
            public int CombinedFlag { get; private set; }
            readonly World<W> _world;
            readonly ComponentArrayGenric<T1>.ComponentArray _components1;
            readonly ComponentArrayGenric<T2>.ComponentArray _components2;
            readonly ComponentArrayGenric<T3>.ComponentArray _components3;

            public Selection<T1, T2, T3> AddFilter<C>() where C : struct
            {
                CombinedFlag |= _world.GetFlag<C>();
                return this;
            }

            internal Selection(World<W> world)
            {
                _flag1 = world.GetFlag<T1>();
                _flag2 = world.GetFlag<T2>();
                _flag3 = world.GetFlag<T3>();

                CombinedFlag = _flag1 | _flag2 | _flag3;
                _world = world;
                _setup = true;

                if (!_world.allComponents.TryGetValue(_flag1, out var cmps))
                {
                    _setup = false;
                    _components1 = null;
                } else
                    _components1 = (cmps as ComponentArrayGenric<T1>).components;

                if (!_world.allComponents.TryGetValue(_flag2, out var cmps2))
                {
                    _setup = false;
                    _components2 = null;
                }
                else
                    _components2 = (cmps2 as ComponentArrayGenric<T2>).components;

                if (!_world.allComponents.TryGetValue(_flag3, out var cmps3))
                {
                    _setup = false;
                    _components3 = null;
                }
                else
                    _components3 = (cmps3 as ComponentArrayGenric<T3>).components;
            }

            public void Run(SystemActionIII<T1, T2, T3> action)
            {
                if (!_setup)
                    return;

                foreach (Entity e in _world.allEntities)
                {
                    EntityComponentsList componentsList = _world[e];

                    if (!componentsList.HasComponents(CombinedFlag))
                        continue;

                    var index1 = componentsList[_flag1].Index;
                    var index2 = componentsList[_flag2].Index;
                    var index3 = componentsList[_flag3].Index;

                    action.Invoke(_components1[index1], _components2[index2], _components3[index3]);
                }
            }

            public void Run(SystemActionRII<T1, T2, T3> action)
            {
                if (!_setup)
                    return;

                foreach (Entity e in _world.allEntities)
                {
                    EntityComponentsList componentsList = _world[e];

                    if (!componentsList.HasComponents(CombinedFlag))
                        continue;

                    var index1 = componentsList[_flag1].Index;
                    var index2 = componentsList[_flag2].Index;
                    var index3 = componentsList[_flag3].Index;

                    var el1 = _components1[index1];
                    action.Invoke(ref el1, _components2[index2], _components3[index3]);
                    _components1[index1] = el1;
                }
            }

            public void Run(SystemActionRRI<T1, T2, T3> action)
            {
                if (!_setup)
                    return;

                foreach (Entity e in _world.allEntities)
                {
                    EntityComponentsList componentsList = _world[e];

                    if (!componentsList.HasComponents(CombinedFlag))
                        continue;

                    var index1 = componentsList[_flag1].Index;
                    var index2 = componentsList[_flag2].Index;
                    var index3 = componentsList[_flag3].Index;

                    var el1 = _components1[index1];
                    var el2 = _components2[index2];
                    action.Invoke(ref el1, ref el2, _components3[index3]);
                    _components1[index1] = el1;
                    _components2[index2] = el2;
                }
            }
           
            public void Run(SystemActionRRR<T1, T2, T3> action)
            {
                if (!_setup)
                    return;

                foreach (Entity e in _world.allEntities)
                {
                    EntityComponentsList componentsList = _world[e];

                    if (!componentsList.HasComponents(CombinedFlag))
                        continue;

                    var index1 = componentsList[_flag1].Index;
                    var index2 = componentsList[_flag2].Index;
                    var index3 = componentsList[_flag3].Index;

                    var el1 = _components1[index1];
                    var el2 = _components2[index2];
                    var el3 = _components3[index3];
                    action.Invoke(ref el1, ref el2,ref el3);
                    _components1[index1] = el1;
                    _components2[index2] = el2;
                    _components3[index3] = el3;
                }
            }

            public void Run(SystemActionRIIE<T1, T2, T3> action)
            {
                if (!_setup)
                    return;

                foreach (Entity e in _world.allEntities)
                {
                    EntityComponentsList componentsList = _world[e];

                    if (!componentsList.HasComponents(CombinedFlag))
                        continue;

                    var index1 = componentsList[_flag1].Index;
                    var index2 = componentsList[_flag2].Index;
                    var index3 = componentsList[_flag3].Index;

                    var el1 = _components1[index1];
                    action.Invoke(ref el1, _components2[index2], _components3[index3], e);
                    _components1[index1] = el1;
                }
            }

            public void Run(SystemActionIIIE<T1, T2, T3> action)
            {
                if (!_setup)
                    return;

                foreach (Entity e in _world.allEntities)
                {
                    EntityComponentsList componentsList = _world[e];

                    if (!componentsList.HasComponents(CombinedFlag))
                        continue;

                    var index1 = componentsList[_flag1].Index;
                    var index2 = componentsList[_flag2].Index;
                    var index3 = componentsList[_flag3].Index;

                    action.Invoke(_components1[index1], _components2[index2], _components3[index3], e);
                }
            }
        }


        private bool TryGet<T1, T2>(
           out ComponentArrayGenric<T1>.ComponentArray tComps1,
           out ComponentArrayGenric<T2>.ComponentArray tComps2,
           out int flagOne, out int flagTwo
           )
           where T1 : struct
           where T2 : struct
        {
            flagOne = GetFlag<T1>();
            flagTwo = GetFlag<T2>();

            if (!allComponents.TryGetValue(flagOne, out var cmps1)
                || !allComponents.TryGetValue(flagTwo, out var cmps2))
            {
                tComps1 = null;
                tComps2 = null;
                return false;
            }

            tComps1 = (cmps1 as ComponentArrayGenric<T1>).components;
            tComps2 = (cmps2 as ComponentArrayGenric<T2>).components;
            return true;
        }

    }
}