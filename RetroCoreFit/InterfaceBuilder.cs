using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace RetroCoreFit
{
    public class InterfaceBuilder
    {

        private Dictionary<Type, object> services = new Dictionary<Type, object>();


        public T Build<T>(HttpClient client = null)
            where T: class
        {
            Type type = typeof(T);
            if (services.TryGetValue(type, out object s)) {
                return (s as T);
            }
            ServiceInterface serviceInterface = CreateInstance(type);
            serviceInterface.client = client;
            serviceInterface.interfaceType = type;
            services[type] = serviceInterface;
            return serviceInterface as T;
        }

        private AssemblyBuilder assemblyBuilder;
        private ModuleBuilder moduleBuilder;

        private ServiceInterface CreateInstance(Type type)
        {

            assemblyBuilder = assemblyBuilder ?? AssemblyBuilder.DefineDynamicAssembly(
                new System.Reflection.AssemblyName("RetroCoreFit2"), AssemblyBuilderAccess.RunAndCollect);
            moduleBuilder = moduleBuilder ?? assemblyBuilder.DefineDynamicModule("RetroCoreFit2");

            TypeBuilder typeBuilder = moduleBuilder.DefineType(
                "A._" + type.Name, 
                System.Reflection.TypeAttributes.Public | TypeAttributes.Class);

            typeBuilder.SetParent(typeof(ServiceInterface));

            MethodInfo invokeMethod = typeof(ServiceInterface).GetMethod("Invoke");

            typeBuilder.AddInterfaceImplementation(type);

            foreach (var property in type.GetProperties()) {
                var p = typeBuilder.DefineProperty(property.Name, PropertyAttributes.None, property.PropertyType, null);
                var fname = $"_{property.Name}";
                var fld = typeBuilder.DefineField(fname, property.PropertyType, FieldAttributes.Private);

                var m = typeBuilder.DefineMethod(property.GetGetMethod().Name, 
                    MethodAttributes.Public | MethodAttributes.Virtual, 
                    property.PropertyType, 
                    null);

                var il = m.GetILGenerator();
                il.Emit(OpCodes.Ldfld, fld);
                il.Emit(OpCodes.Ret);

                typeBuilder.DefineMethodOverride(m,property.GetGetMethod());
                // p.SetGetMethod(m);

                m = typeBuilder.DefineMethod(property.GetSetMethod().Name, MethodAttributes.Public | MethodAttributes.Virtual, 
                    property.GetSetMethod().ReturnType , new Type[] { property.PropertyType });
                il = m.GetILGenerator();
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Stsfld,fld);
                // il.Emit(OpCodes.Ret);
                typeBuilder.DefineMethodOverride(m, property.GetSetMethod());
                // p.SetSetMethod(m);

            }

            foreach (var method in type.GetMethods().Where(x=>!x.IsSpecialName)) {

                var pas = method.GetParameters().Select(x => x.ParameterType).ToArray();

                var m = typeBuilder.DefineMethod(
                        method.Name,
                        method.Attributes ^ MethodAttributes.Abstract,
                        method.CallingConvention,
                        method.ReturnType,
                        method.ReturnParameter.GetRequiredCustomModifiers(),
                        method.ReturnParameter.GetOptionalCustomModifiers(),
                        method.GetParameters().Select(p => p.ParameterType).ToArray(),
                        method.GetParameters().Select(p => p.GetRequiredCustomModifiers()).ToArray(),
                        method.GetParameters().Select(p => p.GetOptionalCustomModifiers()).ToArray()
                        );



                var ilGen = m.GetILGenerator();

                string plist = string.Join(",", method.GetParameters().Select(x => x.ParameterType.FullName));
                string uniqueName = $"{method.Name}+{plist}";

                ilGen.Emit(OpCodes.Nop);
                ilGen.Emit(OpCodes.Ldarg_0);
                ilGen.Emit(OpCodes.Ldstr, uniqueName);

                for (var i = 0; i < method.GetParameters().Length; i++)
                {
                    ilGen.Emit(OpCodes.Ldarg_S, i + 1);
                }
                ilGen.EmitCall(OpCodes.Call, invokeMethod, Type.EmptyTypes);
                ilGen.Emit(OpCodes.Ret);

                typeBuilder.DefineMethodOverride(m,method);

                

            }

            ServiceInterface si = Activator.CreateInstance(typeBuilder.CreateType()) as ServiceInterface;

            return si;
        }
    }

    public class ServiceInterface {

        internal HttpClient client;

        internal Type interfaceType;

        public ServiceInterface()
        {           

            
        }

        public Task<object> Invoke(string method, params object[] plist) {
            return Task.FromResult<object>(null);
        }

    }
}
