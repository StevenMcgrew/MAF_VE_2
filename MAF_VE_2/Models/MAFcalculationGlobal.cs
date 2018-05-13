using System;
using System.Collections.ObjectModel;
using Windows.Data.Json;

namespace MAF_VE_2.Models
{
    class MAFcalculationGlobal
    {
        //public string _id { get; set; }
        //public string _datetime { get; set; }
        public string _year { get; set; }
        public string _make { get; set; }
        public string _model { get; set; }
        public string _engine { get; set; }
        public string _condition { get; set; }
        public string _comments { get; set; }
        public string _mafunits { get; set; }
        public string _tempunits { get; set; }
        public string _altitudeunits { get; set; }
        public string _rpm { get; set; }
        public string _maf { get; set; }
        public string _airtemp { get; set; }
        public string _altitude { get; set; }
        public string _expectedmaf { get; set; }
        public string _mafdiff { get; set; }
        public string _ve { get; set; }

        public MAFcalculationGlobal()
        {
            
        }

        public MAFcalculationGlobal(string jsonString)
        {
            JsonObject jsonObject = JsonObject.Parse(jsonString);
            _year = jsonObject.GetNamedString("_year");
            _make = jsonObject.GetNamedString("_make");
            _model = jsonObject.GetNamedString("_model");
            _engine = jsonObject.GetNamedString("_engine");
            _condition = jsonObject.GetNamedString("_condition");
            _comments = jsonObject.GetNamedString("_comments");
            _mafunits = jsonObject.GetNamedString("_mafunits");
            _tempunits = jsonObject.GetNamedString("_tempunits");
            _altitudeunits = jsonObject.GetNamedString("_altitudeunits");
            _rpm = jsonObject.GetNamedString("_rpm");
            _maf = jsonObject.GetNamedString("_maf");
            _airtemp = jsonObject.GetNamedString("_airtemp");
            _altitude = jsonObject.GetNamedString("_altitude");
            _expectedmaf = jsonObject.GetNamedString("_expectedmaf");
            _mafdiff = jsonObject.GetNamedString("_mafdiff");
            _ve = jsonObject.GetNamedString("_ve");
        }
    }
}