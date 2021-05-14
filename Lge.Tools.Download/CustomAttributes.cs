using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lge.Tools.Download
{
    [AttributeUsage(AttributeTargets.Assembly)]
    public class CustomVesionDateAttribute : Attribute
    {
        string _dateInfo;
        public CustomVesionDateAttribute() : this(string.Empty) { }
        public CustomVesionDateAttribute(string txt) { _dateInfo = txt; }

        public override string ToString()
        {
            return _dateInfo;
        }
    }

    [AttributeUsage(AttributeTargets.Assembly)]
    public class CustomProjectAttribute : Attribute
    {
        string _prjName;
        public CustomProjectAttribute() : this(string.Empty) { }
        public CustomProjectAttribute(string txt) { _prjName = txt; }

        public override string ToString()
        {
            return _prjName;
        }
    }

    [AttributeUsage(AttributeTargets.Assembly)]
    public class CustomRequiredMicomVersionAttribute : Attribute
    {
        string _versionName;
        public CustomRequiredMicomVersionAttribute() : this("None") { }
        public CustomRequiredMicomVersionAttribute(string aVersion)
        {
            _versionName = aVersion;
        }

        public override string ToString()
        {
            return string.Format("Requred MICOM: {0} or later", _versionName);
        }
    }

    [AttributeUsage(AttributeTargets.Assembly)]
    public class CustomRequiredCcmVersionAttribute : Attribute
    {
        string _versionName;
        public CustomRequiredCcmVersionAttribute() : this("None") { }
        public CustomRequiredCcmVersionAttribute(string aVersion)
        {
            _versionName = aVersion;
        }

        public override string ToString()
        {
            return string.Format("Requred CCM: {0} or later", _versionName);
        }
    }
}
