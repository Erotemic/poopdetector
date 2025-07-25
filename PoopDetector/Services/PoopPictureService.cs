﻿using PoopDetector.AI;
using SignInMaui.MSALClient;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PoopDetector.Services
{
    public enum SubmissionType { BeforeCleanup, AfterCleanup }
    public class PoopPicture
    {
        public byte[] File { get; set; }
        public DateTime DateTime { get; set; }
        public string Geolocation { get; set; }
        public string UserId { get; set; }
        public SubmissionType SubmissionType { get; set; }
        // "Pending", "Approved", "Rejected"
        public string Status { get; set; }
        // [{ "x": 100, "y": 150, "w": 50, "h": 75, "c": 0.7 }]
        public string BoundingBoxes { get; set; }
        public static string EnumToString<T>(T enumValue) where T : Enum
        {
            return enumValue.ToString();
        }
    }
    public class PoopPictureService
    {

        public PoopPictureService()
        {
        }
        public async Task<PoopPicture> PreparePicture(PredictionResult result)
        {
            try
            {
                var location = await Geolocation.GetLastKnownLocationAsync();

                if (location == null)
                {
                    // If no location found, get the current location with higher accuracy
                    location = await Geolocation.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.High, TimeSpan.FromSeconds(10)));
                }

                if (PublicClientSingleton.Instance.MSALClientHelper.AuthResult == null)
                {
                    await Task.Run(PublicClientSingleton.Instance.AcquireTokenSilentAsync);
                }

                // Prepare picture data
                var picture = new PoopPicture()
                {
                    File = result.InputImage,
                    Status = "Pending",
                    UserId = PublicClientSingleton.Instance.MSALClientHelper.AuthResult.UniqueId,
                    DateTime = DateTime.Now,
                    SubmissionType = SubmissionType.BeforeCleanup,
                    BoundingBoxes = result.BoundingBoxesToJson()
                };
                if (location == null)
                {
                    Debug.WriteLine("No GPS location available.");
                }
                else
                    picture.Geolocation = $"{location.Latitude};{location.Longitude}";

                return picture;
                //Debug.WriteLine("Picture saved!");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                // throw ex;
                // Handle authentication errors here
            }
            return null;
        }
        // public async Task SendPicture(PoopPicture picture)
        // {
        //     try
        //     {
        //         // TODO: Send the picture somewhere, or save it to a file
        //         Debug.WriteLine("Picture saved!");
        //     }
        //     catch (Exception ex)
        //     {
        //         Debug.WriteLine(ex.Message);
        //         // throw ex;
        //     }
        // }
    }
}
