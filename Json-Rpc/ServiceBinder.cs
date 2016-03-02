﻿namespace AustinHarris.JsonRpc
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using AustinHarris.JsonRpc;

    public static class ServiceBinder
    {
        public static void bindService<T>(string sessionID, Func<T> serviceFactory)
        {
            var instance = serviceFactory();
            var item = instance.GetType(); // var item = typeof(T);
            var regMethod = typeof(Handler).GetMethod("Register");

            var methods = item.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).Where(m => m.GetCustomAttributes(typeof(JsonRpcMethodAttribute), false).Length > 0);
            foreach (var meth in methods)
            {
                Dictionary<string, Type> paras = new Dictionary<string, Type>();
                Dictionary<string, object> defaultValues = new Dictionary<string, object>(); // dictionary that holds default values for optional params.

                var paramzs = meth.GetParameters();

                List<Type> parameterTypeArray = new List<Type>();
                for (int i = 0; i < paramzs.Length; i++)
                {
                    // reflection attribute information for optional parameters
                    //http://stackoverflow.com/questions/2421994/invoking-methods-with-optional-parameters-through-reflection
                    paras.Add(paramzs[i].Name, paramzs[i].ParameterType);

                    if (paramzs[i].IsOptional) // if the parameter is an optional, add the default value to our default values dictionary.
                        defaultValues.Add(paramzs[i].Name, paramzs[i].DefaultValue);
                }

                var resType = meth.ReturnType;
                paras.Add("returns", resType); // add the return type to the generic parameters list.

                var atdata = meth.GetCustomAttributes(typeof(JsonRpcMethodAttribute), false);
                foreach (JsonRpcMethodAttribute handlerAttribute in atdata)
                {
                    var methodName = handlerAttribute.JsonMethodName == string.Empty ? meth.Name : handlerAttribute.JsonMethodName;
                    var newDel = Delegate.CreateDelegate(System.Linq.Expressions.Expression.GetDelegateType(paras.Values.ToArray()), instance /*Need to add support for other methods outside of this instance*/, meth);
                    var handlerSession = Handler.GetSessionHandler(sessionID);
                    regMethod.Invoke(handlerSession, new object[] { methodName, newDel });
                    handlerSession.MetaData.AddService(methodName, paras, defaultValues);
                }
            }
        }
        public static void bindService<T>(string sessionID) where T : new()
        {
            bindService(sessionID, () => new T());
        }
        public static void bindService<T>() where T : new()
        {
            bindService<T>(Handler.DefaultSessionId());
        }
    }
}