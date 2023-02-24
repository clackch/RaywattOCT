using Newtonsoft.Json;
using RaywattApp.Common.Annotation.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace RaywattApp.Common.Annotation.Util
{
    public class AnnotationConverter
    {
        public static void ConvertFromJsonString(string jsonString, out List<Measurement>? measurements, out Measurement? lModeMeasurement)
        {
            measurements = new List<Measurement>();
            lModeMeasurement = new Measurement();

            if (jsonString != null && !string.IsNullOrEmpty(jsonString))
            {
                measurements = JsonConvert.DeserializeObject<List<Measurement>>(jsonString);

                foreach (var measurement in measurements)
                {
                    //Longitude Measurement
                    if (measurement.FrameNumber == -1)
                    {
                        lModeMeasurement = measurement;
                        measurements.Remove(measurement);
                        break;
                    }
                }
            }
        }
    }
}
