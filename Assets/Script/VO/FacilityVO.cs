using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

namespace FacilityVO
{
    public class FacilityInfoVO
    {
        public string facName;
        public string address;
        public float seaLevel;
        public float azimuth;
        public List<Wgs84Info> coords;
        public FacilityEnum facType;
        public FacilityDrawEnum facDrawType;

        public FacilityInfoVO(
            string facName,
            string address,
            float seaLevel,
            float azimuth,
            List<Wgs84Info> coords,
            FacilityEnum facType,
            FacilityDrawEnum facDrawEnum)
        {
            this.facName = facName;
            this.address = address;
            this.seaLevel = seaLevel;
            this.azimuth = azimuth;
            this.coords = coords;
            this.facType = facType;
            this.facDrawType = facDrawEnum;
        }
    }

    public enum FacilityEnum
    {
        ManHole = 0,
        StraightSewer = 1,
        CurveSewer = 2,
        Building = 3,
        Tunnel = 4
    }

    public enum FacilityDrawEnum
    {
        polygon = 0,
        lineString = 1
    }
}