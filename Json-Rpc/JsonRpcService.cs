﻿namespace AustinHarris.JsonRpc
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using AustinHarris.JsonRpc;

    public abstract class JsonRpcService
    {
        protected JsonRpcService()
        {
            buildService(Handler.DefaultSessionId());
        }

        protected JsonRpcService(string sessionID)
        {
            buildService(sessionID);
        }

        private void buildService(string sessionID)
        {
            // get the registerMethod.
            // Method that matches Func<T,R>
            var regMethod = typeof(Handler).GetMethod("Register");

            // var assem = Assembly.GetExecutingAssembly();
            // var TypesWithHandlers = assem.GetTypes().Where(f => f.GetCustomAttributes(typeof(JsonRpcAttribute), false).Length > 0);
            var item = this.GetType();

            var methods = item.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).Where(m => m.GetCustomAttributes(typeof(JsonRpcMethodAttribute), false).Length > 0);
            foreach (var meth in methods)
            {
                Dictionary<string, Type> paras = new Dictionary<string, Type>();
                Dictionary<string, object> defaultValues = new Dictionary<string, object>(); // dictionary that holds default values for optional params.

                var paramzs = meth.GetParameters();

                List<Type> parameterTypeArray = new List<Type>();                
                for (int i = 0; i < paramzs.Length; i++)
                {
                    string paramName;
                    var paramAttrs = paramzs[i].GetCustomAttributes(typeof(JsonRpcParamAttribute), false);
                    if (paramAttrs.Length > 0)
                    {
                        paramName = ((JsonRpcParamAttribute)paramAttrs[0]).JsonParamName;
                    }
                    else
                    {
                        paramName = paramzs[i].Name;
                    }

                    // reflection attribute information for optional parameters
                    //http://stackoverflow.com/questions/2421994/invoking-methods-with-optional-parameters-through-reflection
                    paras.Add(paramName, paramzs[i].ParameterType);

                    if (paramzs[i].IsOptional) // if the parameter is an optional, add the default value to our default values dictionary.
                        defaultValues.Add(paramName, paramzs[i].DefaultValue);
                }

                var resType = meth.ReturnType;
                paras.Add("returns", resType); // add the return type to the generic parameters list.

                var atdata = meth.GetCustomAttributes(typeof(JsonRpcMethodAttribute), false);
                foreach (JsonRpcMethodAttribute handlerAttribute in atdata)
                {
                    var methodName = handlerAttribute.JsonMethodName == string.Empty ? meth.Name : handlerAttribute.JsonMethodName;
                    var newDel = Delegate.CreateDelegate(System.Linq.Expressions.Expression.GetDelegateType(paras.Values.ToArray()), this /*Need to add support for other methods outside of this instance*/, meth);
                    var handlerSession = Handler.GetSessionHandler(sessionID);
                    regMethod.Invoke(handlerSession, new object[] { methodName, newDel });
                    handlerSession.MetaData.AddService(methodName, paras, defaultValues);
                }
            }
        }
    }
}
