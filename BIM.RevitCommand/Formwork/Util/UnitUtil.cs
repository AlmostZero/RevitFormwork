using Autodesk.Revit.DB;

namespace BIM.RevitCommand.Formwork.Util
{
    internal static class UnitUtil
    { /// <summary>
      /// 長さ単位のFeetからMillimeterへ変換
      /// </summary>
      /// <param name="feet"></param>
      /// <returns></returns>
        public static double Feet_To_Millimeters( double feet )
        {
            double meter = UnitUtils.Convert( feet,
                                              UnitTypeId.Feet,
                                              UnitTypeId.Millimeters );
            return meter;
        }



        /// <summary>
        /// 長さ単位のMillimeterからFeetへ変換
        /// </summary>
        /// <param name="meter"></param>
        /// <returns></returns>
        public static double Millimeters_To_Feet( double meter )
        {
            double feet = UnitUtils.Convert( meter,
                                             UnitTypeId.Millimeters,
                                             UnitTypeId.Feet );
            return feet;
        }

        /// <summary>
        /// 面積単位のFeetからMeterへ変換
        /// </summary>
        /// <param name="areaFeet"></param>
        /// <returns></returns>
        public static double Area_Feet_To_Area_Meter( double areaFeet )
        {
            double areaMeter = UnitUtils.Convert( areaFeet,
                                                  UnitTypeId.SquareFeet,
                                                  UnitTypeId.SquareMeters );
            return areaMeter;
        }

        /// <summary>
        /// 面積単位のMeterからFeetへ変換
        /// </summary>
        /// <param name="areaMeter"></param>
        /// <returns></returns>
        public static double Area_Meter_To_Area_Feet( double areaMeter )
        {
            double areaFeet = UnitUtils.Convert( areaMeter,
                                                 UnitTypeId.SquareMeters,
                                                 UnitTypeId.SquareFeet );
            return areaFeet;
        }

        /// <summary>
        /// 体積単位のFeetからMeterへ変換
        /// </summary>
        /// <param name="cubicFeet"></param>
        /// <returns></returns>
        public static double Cubic_Feet_To_Cubic_Meter( double cubicFeet )
        {
            double cubicMeter = UnitUtils.Convert( cubicFeet,
                                                   UnitTypeId.CubicFeet,
                                                   UnitTypeId.CubicMeters );
            return cubicMeter;
        }

        /// <summary>
        /// 体積単位のMeterからFeetへ変換
        /// </summary>
        /// <param name="cubicMeter"></param>
        /// <returns></returns>
        public static double Cubic_Meter_To_Cubic_Feet( double cubicMeter )
        {
            double cubicFeet = UnitUtils.Convert( cubicMeter,
                                                  UnitTypeId.CubicMeters,
                                                  UnitTypeId.CubicFeet );
            return cubicFeet;
        }

    }
}
