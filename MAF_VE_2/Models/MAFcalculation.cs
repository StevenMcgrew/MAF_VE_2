using SQLite.Net.Attributes;

namespace MAF_VE_2.Models
{
    class MAFcalculation
    {
        [PrimaryKey, AutoIncrement]
        public int vehicleID { get; set; }

        public string Year { get; set; }
        public string Make { get; set; }
        public string Model { get; set; }
        public string Engine { get; set; }
        public string Condition { get; set; }
        public string Comments { get; set; }

        public string MAF_units { get; set; }
        public string Temp_units { get; set; }
        public string Altitude_units { get; set; }

        public double Engine_speed { get; set; }
        public double MAF { get; set; }
        public double Engine_size { get; set; }
        public double Air_temperature { get; set; }
        public double Altitude { get; set; }
        public double Expected_MAF { get; set; }
        public double MAF_Difference { get; set; }
        public double Volumetric_Efficiency { get; set; }

        public MAFcalculation()
        {
            
        }

        public MAFcalculation(MAFcalculation mafCalculation)
        {
            vehicleID = mafCalculation.vehicleID;
            Year = mafCalculation.Year;
            Make = mafCalculation.Make;
            Model = mafCalculation.Model;
            Engine = mafCalculation.Engine;
            Condition = mafCalculation.Condition;
            Comments = mafCalculation.Comments;
            MAF_units = mafCalculation.MAF_units;
            Temp_units = mafCalculation.Temp_units;
            Altitude_units = mafCalculation.Altitude_units;
            Engine_speed = mafCalculation.Engine_speed;
            MAF = mafCalculation.MAF;
            Engine_size = mafCalculation.Engine_size;
            Air_temperature = mafCalculation.Air_temperature;
            Altitude = mafCalculation.Altitude;
            Expected_MAF = mafCalculation.Expected_MAF;
            MAF_Difference = mafCalculation.MAF_Difference;
            Volumetric_Efficiency = mafCalculation.Volumetric_Efficiency;
        }
    }
}
