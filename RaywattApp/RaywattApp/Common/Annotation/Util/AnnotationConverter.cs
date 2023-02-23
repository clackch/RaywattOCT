using Newtonsoft.Json;
using RaywattApp.Common.Annotation.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace RaywattApp.Common.Annotation.Util
{
    public class AnnotationConverter
    {
        public static void ConvertFromJsonString(string jsonString, out List<Measurement>? measurements, out ObservableCollection<LengthGeometry>? lModeLengths, out List<TextGeometry>? lModeTexts)
        {
            measurements = new List<Measurement>();
            lModeLengths = new ObservableCollection<LengthGeometry>();
            lModeTexts = new List<TextGeometry>();

            if (jsonString != null && !string.IsNullOrEmpty(jsonString))
            {
                measurements = JsonConvert.DeserializeObject<List<Measurement>>(jsonString);

                foreach (var measurement in measurements)
                {
                    //Longitude Measurement
                    if (measurement.FrameNumber == -1)
                    {
                        lModeLengths = measurement.LengthGeometries;
                        lModeTexts = measurement.TextGeometries;
                        measurements.Remove(measurement);
                        break;
                    }
                }
            }
        }
    }
}
