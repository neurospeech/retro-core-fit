using System;

namespace RetroCoreFit
{

    public class NamedAttribute : Attribute {

        public NamedAttribute(string name)
        {
            this.Name = name;
        }

        public string Name { get; }

    }

    [System.AttributeUsage(AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Parameter, Inherited = false, AllowMultiple = true)]
    public sealed class HeaderAttribute : NamedAttribute
    {
        public HeaderAttribute(string name) : base(name)
        {
        }
    }

    public abstract class HttpMethodAttribute : NamedAttribute
    {
        public HttpMethodAttribute(string name, string path) : base(name)
        {
            this.Path = path;
        }


        public string Path { get;  }
    }


    [System.AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
    public sealed class PutAttribute : HttpMethodAttribute
    {
        public PutAttribute(string name) : base(name, "PUT")
        {
        }
    }


    [System.AttributeUsage(AttributeTargets.Interface, Inherited = false, AllowMultiple = true)]
    public sealed class BaseUrlAttribute : NamedAttribute
    {
        public BaseUrlAttribute(string name) : base(name)
        {
        }
    }

    [System.AttributeUsage(AttributeTargets.Parameter, Inherited = false, AllowMultiple = true)]
    public sealed class BodyAttribute : Attribute {
    }


    public class ParamAttribute : Attribute {
        public ParamAttribute()
        {

        }

        public ParamAttribute(string name)
        {
            this.Name = name;
        }

        public string Name { get; set; }
    }


    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class QueryAttribute : ParamAttribute {
        public QueryAttribute()
        {

        }

        public QueryAttribute(string name):base(name)
        {

        }
    }


    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class PathAttribute : ParamAttribute
    {
        public PathAttribute()
        {

        }

        public PathAttribute(string name) : base(name)
        {

        }
    }

}
