﻿// COPYRIGHT 2009, 2010, 2011, 2012, 2013 by the Open Rails project.
// 
// This file is part of Open Rails.
// 
// Open Rails is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Open Rails is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Open Rails.  If not, see <http://www.gnu.org/licenses/>.

// This file is the responsibility of the 3D & Environment Team. 

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ORTS.Processes;
using ORTS.Settings;

namespace ORTS.Viewer3D
{
    #region MSTSSkyConstants
    static class MSTSSkyConstants
    {
        // Sky dome constants
        public const int skyRadius = 6000;
        public const int skySides = 24;
        public static bool IsNight = false;
    }

    #endregion

    #region MSTSSkyDrawer
    public class MSTSSkyDrawer
    {

        Viewer MSTSSkyViewer;
        Material MSTSSkyMaterial;

        // Classes reqiring instantiation
        public MSTSSkyMesh MSTSSkyMesh;
        WorldLatLon mstsskyworldLoc; // Access to latitude and longitude calcs (MSTS routes only)
        SunMoonPos MSTSSkyVectors;

		int mstsskyseasonType; //still need to remember it as MP now can change it.
        #region Class variables
        // Latitude of current route in radians. -pi/2 = south pole, 0 = equator, pi/2 = north pole.
        // Longitude of current route in radians. -pi = west of prime, 0 = prime, pi = east of prime.
        public double mstsskylatitude, mstsskylongitude;
        // Date of activity

        public ORTS.Viewer3D.SkyViewer.Date date;
  
        // Size of the sun- and moon-position lookup table arrays.
        // Must be an integral divisor of 1440 (which is the number of minutes in a day).
        private int maxSteps = 72;
        private double mstsskyoldClockTime;
        private int step1, step2;
        // Phase of the moon
        public int mstsskymoonPhase;
        // Wind speed and direction
        public float mstsskywindSpeed;
        public float mstsskywindDirection;
        // Overcast level
        public float mstsskyovercastFactor;
        // Fog distance
        public float mstsskyfogDistance;
        public bool isNight = false;

        public List<string> SkyLayers = new List<string>();

        // These arrays and vectors define the position of the sun and moon in the world
        Vector3[] mstsskysolarPosArray = new Vector3[72];
        Vector3[] mstsskylunarPosArray = new Vector3[72];
        public Vector3 mstsskysolarDirection;
        public Vector3 mstsskylunarDirection;
        #endregion

        #region Constructor
        /// <summary>
        /// SkyDrawer constructor
        /// </summary>
        public MSTSSkyDrawer(Viewer viewer)
        {
            MSTSSkyViewer = viewer;
            MSTSSkyMaterial = viewer.MaterialManager.Load("MSTSSky");
            // Instantiate classes
            MSTSSkyMesh = new MSTSSkyMesh( MSTSSkyViewer.RenderProcess, MSTSSkyViewer);
            MSTSSkyVectors = new SunMoonPos();

            //viewer.World.MSTSSky.MSTSSkyMaterial.Viewer.MaterialManager.sunDirection.Y < 0
            // Set default values
            mstsskyseasonType = (int)MSTSSkyViewer.Simulator.Season;
            date.ordinalDate = 82 + mstsskyseasonType * 91;
            // TODO: Set the following three externally from ORTS route files (future)
            date.month = 1 + date.ordinalDate / 30;
            date.day = 21;
            date.year = 2010;
            // Default wind speed and direction
            mstsskywindSpeed = 5.0f; // m/s (approx 11 mph)
            mstsskywindDirection = 4.7f; // radians (approx 270 deg, i.e. westerly)
       }
        #endregion

        /// <summary>
        /// Used to update information affecting the SkyMesh
        /// </summary>
        public void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
        {
			if (mstsskyseasonType != (int)MSTSSkyViewer.Simulator.Season)
			{
				mstsskyseasonType = (int)MSTSSkyViewer.Simulator.Season;
				date.ordinalDate = 82 + mstsskyseasonType * 91;
				// TODO: Set the following three externally from ORTS route files (future)
				date.month = 1 + date.ordinalDate / 30;
				date.day = 21;
				date.year = 2010;
			}
            // Adjust dome position so the bottom edge is not visible
			Vector3 ViewerXNAPosition = new Vector3(MSTSSkyViewer.Camera.Location.X, MSTSSkyViewer.Camera.Location.Y - 100, -MSTSSkyViewer.Camera.Location.Z);
            Matrix XNASkyWorldLocation = Matrix.CreateTranslation(ViewerXNAPosition);

            if (mstsskyworldLoc == null)
            {
                // First time around, initialize the following items:
                mstsskyworldLoc = new WorldLatLon();
                mstsskyoldClockTime = MSTSSkyViewer.Simulator.ClockTime % 86400;
                while (mstsskyoldClockTime < 0) mstsskyoldClockTime += 86400;
                step1 = step2 = (int)(mstsskyoldClockTime / 1200);
                step2++;
                // Get the current latitude and longitude coordinates
                mstsskyworldLoc.ConvertWTC(MSTSSkyViewer.Camera.TileX, MSTSSkyViewer.Camera.TileZ, MSTSSkyViewer.Camera.Location, ref mstsskylatitude, ref mstsskylongitude);
                // Fill in the sun- and moon-position lookup tables
                for (int i = 0; i < maxSteps; i++)
                {
                    mstsskysolarPosArray[i] = SunMoonPos.SolarAngle(mstsskylatitude, mstsskylongitude, ((float)i / maxSteps), date);
                    mstsskylunarPosArray[i] = SunMoonPos.LunarAngle(mstsskylatitude, mstsskylongitude, ((float)i / maxSteps), date);
                }
                // Phase of the moon is generated at random
                Random random = new Random();
                mstsskymoonPhase = random.Next(8);
                if (mstsskymoonPhase == 6 && date.ordinalDate > 45 && date.ordinalDate < 330)
                    mstsskymoonPhase = 3; // Moon dog only occurs in winter
                // Overcast factor: 0.0=almost no clouds; 0.1=wispy clouds; 1.0=total overcast
                //mstsskyovercastFactor = MSTSSkyViewer.World.WeatherControl.overcastFactor;
                mstsskyfogDistance = MSTSSkyViewer.World.WeatherControl.fogDistance;
            }

			if (MultiPlayer.MPManager.IsClient() && MultiPlayer.MPManager.Instance().weatherChanged)
			{
				//received message about weather change
				if ( MultiPlayer.MPManager.Instance().overCast >= 0)
				{
					mstsskyovercastFactor = MultiPlayer.MPManager.Instance().overCast;
				}
                //received message about weather change
                if (MultiPlayer.MPManager.Instance().newFog > 0)
                {
                    mstsskyfogDistance = MultiPlayer.MPManager.Instance().newFog;
                }
                try
                {
                    if (MultiPlayer.MPManager.Instance().overCast >= 0 || MultiPlayer.MPManager.Instance().newFog > 0) 
                    {
                        MultiPlayer.MPManager.Instance().weatherChanged = false;
                        MultiPlayer.MPManager.Instance().overCast = -1 ;
                        MultiPlayer.MPManager.Instance().newFog = -1 ;
                    }
                }
                catch { }

            }

////////////////////// T E M P O R A R Y ///////////////////////////

            // The following keyboard commands are used for viewing sky and weather effects in "demo" mode.
            // Control- and Control+ for overcast, Shift- and Shift+ for fog and - and + for time.

            // Don't let multiplayer clients adjust the weather.
            if (!MultiPlayer.MPManager.IsClient())
            {
                // Overcast ranges from 0 (completely clear) to 1 (completely overcast).
                if (UserInput.IsDown(UserCommands.DebugOvercastIncrease)) mstsskyovercastFactor = MathHelper.Clamp(mstsskyovercastFactor + elapsedTime.RealSeconds / 10, 0, 1);
                if (UserInput.IsDown(UserCommands.DebugOvercastDecrease)) mstsskyovercastFactor = MathHelper.Clamp(mstsskyovercastFactor - elapsedTime.RealSeconds / 10, 0, 1);
                // Fog ranges from 10m (can't see anything) to 100km (clear arctic conditions).
                if (UserInput.IsDown(UserCommands.DebugFogIncrease)) mstsskyfogDistance = MathHelper.Clamp(mstsskyfogDistance - elapsedTime.RealSeconds * mstsskyfogDistance, 10, 100000);
                if (UserInput.IsDown(UserCommands.DebugFogDecrease)) mstsskyfogDistance = MathHelper.Clamp(mstsskyfogDistance + elapsedTime.RealSeconds * mstsskyfogDistance, 10, 100000);
            }
            // Don't let clock shift if multiplayer.
            if (!MultiPlayer.MPManager.IsMultiPlayer())
            {
                // Shift the clock forwards or backwards at 1h-per-second.
                if (UserInput.IsDown(UserCommands.DebugClockForwards)) MSTSSkyViewer.Simulator.ClockTime += elapsedTime.RealSeconds * 3600;
                if (UserInput.IsDown(UserCommands.DebugClockBackwards)) MSTSSkyViewer.Simulator.ClockTime -= elapsedTime.RealSeconds * 3600;
                if (MSTSSkyViewer.World.Precipitation != null && (UserInput.IsDown(UserCommands.DebugClockForwards) || UserInput.IsDown(UserCommands.DebugClockBackwards))) MSTSSkyViewer.World.Precipitation.Reset();
            }
            // Server needs to notify clients of weather changes.
            if (MultiPlayer.MPManager.IsServer())
            {
                if (UserInput.IsReleased(UserCommands.DebugOvercastIncrease) || UserInput.IsReleased(UserCommands.DebugOvercastDecrease) || UserInput.IsReleased(UserCommands.DebugFogIncrease) || UserInput.IsReleased(UserCommands.DebugFogDecrease))
                {
                    MultiPlayer.MPManager.Instance().SetEnvInfo(mstsskyovercastFactor, mstsskyfogDistance);
                    MultiPlayer.MPManager.Notify((new MultiPlayer.MSGWeather(-1, mstsskyovercastFactor, mstsskyfogDistance, -1)).ToString());
                }
            }

////////////////////////////////////////////////////////////////////

            // Current solar and lunar position are calculated by interpolation in the lookup arrays.
            // Using the Lerp() function, so need to calculate the in-between differential
            float diff = (float)(MSTSSkyViewer.Simulator.ClockTime - mstsskyoldClockTime) / 1200;
            // The rest of this increments/decrements the array indices and checks for overshoot/undershoot.
            if (MSTSSkyViewer.Simulator.ClockTime >= (mstsskyoldClockTime + 1200)) // Plus key, or normal forward in time
            {
                step1++;
                step2++;
                mstsskyoldClockTime = MSTSSkyViewer.Simulator.ClockTime;
                diff = 0;
                if (step1 == maxSteps - 1) // Midnight. Value is 71 for maxSteps = 72
                {
                    step2 = 0;
                }
                if (step1 == maxSteps) // Midnight.
                {
                    step1 = 0;
                }
            }
            if (MSTSSkyViewer.Simulator.ClockTime <= (mstsskyoldClockTime - 1200)) // Minus key
            {
                step1--;
                step2--;
                mstsskyoldClockTime = MSTSSkyViewer.Simulator.ClockTime;
                diff = 0;
                if (step1 < 0) // Midnight.
                {
                    step1 = 71;
                }
                if (step2 < 0) // Midnight.
                {
                    step2 = 71;
                }
            }
            

            mstsskysolarDirection.X = MathHelper.Lerp(mstsskysolarPosArray[step1].X, mstsskysolarPosArray[step2].X, diff);
            mstsskysolarDirection.Y = MathHelper.Lerp(mstsskysolarPosArray[step1].Y, mstsskysolarPosArray[step2].Y, diff);
            mstsskysolarDirection.Z = MathHelper.Lerp(mstsskysolarPosArray[step1].Z, mstsskysolarPosArray[step2].Z, diff);
            mstsskylunarDirection.X = MathHelper.Lerp(mstsskylunarPosArray[step1].X, mstsskylunarPosArray[step2].X, diff);
            mstsskylunarDirection.Y = MathHelper.Lerp(mstsskylunarPosArray[step1].Y, mstsskylunarPosArray[step2].Y, diff);
            mstsskylunarDirection.Z = MathHelper.Lerp(mstsskylunarPosArray[step1].Z, mstsskylunarPosArray[step2].Z, diff);

            frame.AddPrimitive(MSTSSkyMaterial, MSTSSkyMesh, RenderPrimitiveGroup.Sky, ref XNASkyWorldLocation);
        }

        [CallOnThread("Loader")]
        internal void Mark()
        {
            MSTSSkyMaterial.Mark();
        }
    }
    #endregion

    #region MSTSSkyMesh
    public class MSTSSkyMesh: RenderPrimitive 
    {
        private VertexBuffer MSTSSkyVertexBuffer;
        private static VertexDeclaration MSTSSkyVertexDeclaration;
        private static IndexBuffer MSTSSkyIndexBuffer;
        private static int MSTSSkyVertexStride;  // in bytes
        public int drawIndex;

        VertexPositionNormalTexture[] vertexList;
        private static short[] triangleListIndices; // Trilist buffer.
        // Sky dome geometry is based on two global variables: the radius and the number of sides
        public int mstsskyRadius = MSTSSkyConstants.skyRadius;
        public int mstsskyradius;
        private static int mstsskySides = MSTSSkyConstants.skySides;
        // skyLevels: Used for iterating vertically through the "levels" of the hemisphere polygon
        private static int mstsskyLevels = ((MSTSSkyConstants.skySides / 4) - 1);
        // Number of vertices in the sky hemisphere. (each dome = 145 for 24-sided sky dome: 24 x 6 + 1)
        // plus four more for the moon quad
        private static int numVertices = 4 + 2 * (int)((Math.Pow(mstsskySides, 2) / 4) + 1);
        // Number of point indices (each dome = 792 for 24 sides: 5 levels of 24 triangle pairs each
        // plus 24 triangles at the zenith)
        // plus six more for the moon quad
        private static short indexCount = 6 + 2 * ((MSTSSkyConstants.skySides * 2 * 3 * ((MSTSSkyConstants.skySides / 4) - 1)) + 3 * MSTSSkyConstants.skySides);

        /// <summary>
        /// Constructor.
        /// </summary>
        /// 

        public MSTSSkyMesh(RenderProcess renderProcess, Viewer viewer)
        {
            var tileFactor = 1;

            if (viewer.ENVFile.SkyLayers != null)
            {
                mstsskyradius = viewer.ENVFile.SkyLayers[0]._top_radius;
            }

            mstsskyRadius = MSTSSkyConstants.skyRadius; 

            if ( MSTSSkyConstants.IsNight == true) 
                    tileFactor = 8;
                else
                    tileFactor = 1;
            // Initialize the vertex and point-index buffers
            vertexList = new VertexPositionNormalTexture[numVertices];
            triangleListIndices = new short[indexCount];

            // Sky dome
            MSTSSkyDomeVertexList(0, mstsskyRadius, tileFactor);
            MSTSSkyDomeTriangleList(0, 0);
            // Moon quad
            MoonLists(numVertices - 5, indexCount - 6);//(144, 792);
            // Meshes have now been assembled, so put everything into vertex and index buffers
            InitializeVertexBuffers(renderProcess.GraphicsDevice);
            
        }

        public override void Draw(GraphicsDevice graphicsDevice)
        {
            graphicsDevice.VertexDeclaration = MSTSSkyVertexDeclaration;
            graphicsDevice.Vertices[0].SetSource(MSTSSkyVertexBuffer, 0, MSTSSkyVertexStride);
            graphicsDevice.Indices = MSTSSkyIndexBuffer;

            switch (drawIndex)
            {
                case 1: // Sky dome
                    graphicsDevice.DrawIndexedPrimitives(
                        PrimitiveType.TriangleList,
                        0,
                        0,
                        (numVertices - 4) / 2,
                        0,
                        (indexCount - 6) / 6);
                    break;
                case 2: // Moon
                    graphicsDevice.DrawIndexedPrimitives(
                    PrimitiveType.TriangleList,
                    0,
                    numVertices - 4,
                    4,
                    indexCount - 6,
                    2);
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Creates the vertex list for each sky dome.
        /// </summary>
        /// <param name="index">The starting vertex number</param>
        /// <param name="radius">The radius of the dome</param>
        /// <param name="oblate">The amount the dome is flattened</param>
        private void MSTSSkyDomeVertexList(int index, int radius, float texturetiling)
        {
            int vertexIndex = index;
            // for each vertex
            for (int i = 0; i < (mstsskySides / 4); i++) // (=6 for 24 sides)
                for (int j = 0; j < mstsskySides; j++) // (=24 for top overlay)
                {
                    float y = (float)Math.Sin(MathHelper.ToRadians((360 / mstsskySides) * i)) * radius;
                    float yRadius = radius * (float)Math.Cos(MathHelper.ToRadians((360 / mstsskySides) * i));
                    float x = (float)Math.Cos(MathHelper.ToRadians((360 / mstsskySides) * (mstsskySides - j))) * yRadius;
                    float z = (float)Math.Sin(MathHelper.ToRadians((360 / mstsskySides) * (mstsskySides - j))) * yRadius;

                    // UV coordinates - top overlay
                    float uvRadius;
                    uvRadius = 0.5f - (float)(0.5f * i) / (mstsskySides / 4);
                    float uv_u = 0.5f - ((float)Math.Cos(MathHelper.ToRadians((360 / mstsskySides) * (mstsskySides - j))) * uvRadius);
                    float uv_v = 0.5f - ((float)Math.Sin(MathHelper.ToRadians((360 / mstsskySides) * (mstsskySides - j))) * uvRadius);

                    // Store the position, texture coordinates and normal (normalized position vector) for the current vertex
                    vertexList[vertexIndex].Position = new Vector3(x, y, z);
                    vertexList[vertexIndex].TextureCoordinate = new Vector2(uv_u * texturetiling , uv_v * texturetiling);  ///MSTS Sky is tiled, need to add that in.
                    vertexList[vertexIndex].Normal = Vector3.Normalize(new Vector3(x, y, z));
                    vertexIndex++;
                }
            // Single vertex at zenith

            vertexList[vertexIndex].Position = new Vector3(0, radius, 0);
            vertexList[vertexIndex].Normal = new Vector3(0, 1, 0);
            vertexList[vertexIndex].TextureCoordinate = new Vector2(0.5f, 0.5f); // (top overlay)
        }

        /// <summary>
        /// Creates the triangle index list for each dome.
        /// </summary>
        /// <param name="index">The starting triangle index number</param>
        /// <param name="pass">A multiplier used to arrive at the starting vertex number</param>
        static void MSTSSkyDomeTriangleList(short index, short pass)
        {
            // ----------------------------------------------------------------------
            // 24-sided sky dome mesh is built like this:        48 49 50
            // Triangles are wound couterclockwise          71 o--o--o--o
            // because we're looking at the inner              | /|\ | /|
            // side of the hemisphere. Each time               |/ | \|/ |
            // we circle around to the start point          47 o--o--o--o 26
            // on the mesh we have to reset the                |\ | /|\ |
            // vertex number back to the beginning.            | \|/ | \|
            // Using WAC's sw,se,nw,ne coordinate    nw ne  23 o--o--o--o 
            // convention.-->                        sw se        0  1  2
            // ----------------------------------------------------------------------
            short iIndex = index;
            short baseVert = (short)(pass * (short)((numVertices - 4) / 2));
            for (int i = 0; i < mstsskyLevels; i++) // (=5 for 24 sides)
                for (int j = 0; j < mstsskySides; j++) // (=24 for 24 sides)
                {
                    // Vertex indices, beginning in the southwest corner
                    short sw = (short)(baseVert + (j + i * (mstsskySides)));
                    short nw = (short)(sw + mstsskySides); // top overlay mapping
                    short ne = (short)(nw + 1);

                    short se = (short)(sw + 1);

                    if (((i & 1) == (j & 1)))  // triangles alternate
                    {
                        triangleListIndices[iIndex++] = sw;
                        triangleListIndices[iIndex++] = ((ne - baseVert) % mstsskySides == 0) ? (short)(ne - mstsskySides) : ne;
                        triangleListIndices[iIndex++] = nw;
                        triangleListIndices[iIndex++] = sw;
                        triangleListIndices[iIndex++] = ((se - baseVert) % mstsskySides == 0) ? (short)(se - mstsskySides) : se;
                        triangleListIndices[iIndex++] = ((ne - baseVert) % mstsskySides == 0) ? (short)(ne - mstsskySides) : ne;
                    }
                    else
                    {
                        triangleListIndices[iIndex++] = sw;
                        triangleListIndices[iIndex++] = ((se - baseVert) % mstsskySides == 0) ? (short)(se - mstsskySides) : se;
                        triangleListIndices[iIndex++] = nw;
                        triangleListIndices[iIndex++] = ((se - baseVert) % mstsskySides == 0) ? (short)(se - mstsskySides) : se;
                        triangleListIndices[iIndex++] = ((ne - baseVert) % mstsskySides == 0) ? (short)(ne - mstsskySides) : ne;
                        triangleListIndices[iIndex++] = nw;
                    }
                }
            //Zenith triangles (=24 for 24 sides)
            for (int i = 0; i < mstsskySides; i++)
            {
                short sw = (short)(baseVert + (((mstsskySides) * mstsskyLevels) + i));
                short se = (short)(sw + 1);

                triangleListIndices[iIndex++] = sw;
                triangleListIndices[iIndex++] = ((se - baseVert) % mstsskySides == 0) ? (short)(se - mstsskySides) : se;
                triangleListIndices[iIndex++] = (short)(baseVert + (short)((numVertices - 5) / 2)); // The zenith
            }
        }

        /// <summary>
        /// Creates the moon vertex and triangle index lists.
        /// <param name="vertexIndex">The starting vertex number</param>
        /// <param name="iIndex">The starting triangle index number</param>
        /// </summary>
        private void MoonLists(int vertexIndex, int iIndex)
        {
            // Moon vertices
            for (int i = 0; i < 2; i++)
                for (int j = 0; j < 2; j++)
                {
                    vertexIndex++;
                    vertexList[vertexIndex].Position = new Vector3(i, j, 0);
                    vertexList[vertexIndex].Normal = new Vector3(0, 0, 1);
                    vertexList[vertexIndex].TextureCoordinate = new Vector2(i, j);
                }

            // Moon indices - clockwise winding
            short msw = (short)(numVertices - 4);
            short mnw = (short)(msw + 1);
            short mse = (short)(mnw + 1);
            short mne = (short)(mse + 1);
            triangleListIndices[iIndex++] = msw;
            triangleListIndices[iIndex++] = mnw;
            triangleListIndices[iIndex++] = mse;
            triangleListIndices[iIndex++] = mse;
            triangleListIndices[iIndex++] = mnw;
            triangleListIndices[iIndex++] = mne;
        }

        /// <summary>
        /// Initializes the sky dome, cloud dome and moon vertex and triangle index list buffers.
        /// </summary>
        private void InitializeVertexBuffers(GraphicsDevice graphicsDevice)
        {
            if (MSTSSkyVertexDeclaration == null)
            {
                MSTSSkyVertexDeclaration = new VertexDeclaration(graphicsDevice, VertexPositionNormalTexture.VertexElements);
                MSTSSkyVertexStride = VertexPositionNormalTexture.SizeInBytes;
            }
            // Initialize the vertex and index buffers, allocating memory for each vertex and index
            MSTSSkyVertexBuffer = new VertexBuffer(graphicsDevice, VertexPositionNormalTexture.SizeInBytes * vertexList.Length, BufferUsage.WriteOnly);
            MSTSSkyVertexBuffer.SetData(vertexList);
            if (MSTSSkyIndexBuffer == null)
            {
                MSTSSkyIndexBuffer = new IndexBuffer(graphicsDevice, typeof(short), indexCount, BufferUsage.WriteOnly);
                MSTSSkyIndexBuffer.SetData(triangleListIndices);
            }
        }

    } // SkyMesh
    #endregion

    #region MSTSSkyMaterial
    public class MSTSSkyMaterial : Material
    {
        SkyShader MSTSSkyShader;
        Texture2D MSTSSkyTexture;
        Texture2D MSTSSkylowTexture;
        Texture2D MSTSSkyStarTextureN;
        Texture2D MSTSSkyStarTextureS;
        Texture2D MSTSSkyMoonTexture;
        Texture2D MSTSSkyMoonMask;
        Texture2D MSTSSkyCloudTexture;
        Texture2D MSTSSkySunTexture;
        private Matrix XNAMoonMatrix;
        IEnumerator<EffectPass> ShaderPassesSky;
        IEnumerator<EffectPass> ShaderPassesMoon;
        IEnumerator<EffectPass> ShaderPassesClouds;

        public MSTSSkyMaterial(Viewer viewer)
            : base(viewer, null)
        {
            MSTSSkyShader = Viewer.MaterialManager.SkyShader;
            // TODO: This should happen on the loader thread. 
            if (viewer.ENVFile.SkyLayers != null)
            {
                var mstsskytexture = Viewer.ENVFile.SkyLayers.ToArray();
                string mstsSkyTexture = Viewer.Simulator.RoutePath + @"\envfiles\textures\" + mstsskytexture[0].TextureName.ToString();
                MSTSSkyTexture = MSTS.Formats.ACEFile.Texture2DFromFile(Viewer.RenderProcess.GraphicsDevice, mstsSkyTexture);
                string mstsSkyStarTexture = Viewer.Simulator.RoutePath + @"\envfiles\textures\" + mstsskytexture[1].TextureName.ToString();
                MSTSSkyStarTextureN = MSTS.Formats.ACEFile.Texture2DFromFile(Viewer.RenderProcess.GraphicsDevice, mstsSkyStarTexture);
                MSTSSkyStarTextureS = MSTS.Formats.ACEFile.Texture2DFromFile(Viewer.RenderProcess.GraphicsDevice, mstsSkyStarTexture);  //ToDo:  MSTS doesn't use multiple star textures, revisit this for OR env files possibly in the future.
                string mstsSkylowTexture = Viewer.Simulator.RoutePath + @"\envfiles\textures\" + mstsskytexture[1].TextureName.ToString();
                MSTSSkylowTexture = MSTS.Formats.ACEFile.Texture2DFromFile(Viewer.RenderProcess.GraphicsDevice, mstsSkylowTexture);
            }
            else
            {
                MSTSSkyTexture = Texture2D.FromFile(Viewer.RenderProcess.GraphicsDevice, System.IO.Path.Combine(Viewer.ContentPath, "SkyDome1.png"));
                MSTSSkyStarTextureN = Texture2D.FromFile(Viewer.RenderProcess.GraphicsDevice, System.IO.Path.Combine(Viewer.ContentPath, "Starmap_N.png"));
                MSTSSkyStarTextureS = Texture2D.FromFile(Viewer.RenderProcess.GraphicsDevice, System.IO.Path.Combine(Viewer.ContentPath, "Starmap_S.png"));
            }
            if (viewer.ENVFile.SkySatellite != null)
            {
                var mstsskysatellitetexture = Viewer.ENVFile.SkySatellite.ToArray();
                string mstsSkySunTexture = Viewer.Simulator.RoutePath + @"\envfiles\textures\" + mstsskysatellitetexture[0].TextureName.ToString();
                string mstsSkyMoonTexture = Viewer.Simulator.RoutePath + @"\envfiles\textures\" + mstsskysatellitetexture[1].TextureName.ToString();
                MSTSSkySunTexture = MSTS.Formats.ACEFile.Texture2DFromFile(Viewer.RenderProcess.GraphicsDevice, mstsSkySunTexture);
                MSTSSkyMoonTexture = MSTS.Formats.ACEFile.Texture2DFromFile(Viewer.RenderProcess.GraphicsDevice, mstsSkyMoonTexture);
            }
            else
                MSTSSkyMoonTexture = Texture2D.FromFile(Viewer.RenderProcess.GraphicsDevice, System.IO.Path.Combine(Viewer.ContentPath, "MoonMap.png"));

            MSTSSkyMoonMask = Texture2D.FromFile(Viewer.RenderProcess.GraphicsDevice, System.IO.Path.Combine(Viewer.ContentPath, "MoonMask.png")); //ToDo:  No MSTS equivalent - will need to be fixed in MSTSSky.cs
            MSTSSkyCloudTexture = Texture2D.FromFile(Viewer.RenderProcess.GraphicsDevice, System.IO.Path.Combine(Viewer.ContentPath, "Clouds01.png"));

            ShaderPassesSky = MSTSSkyShader.Techniques["Sky"].Passes.GetEnumerator();
            ShaderPassesMoon = MSTSSkyShader.Techniques["Moon"].Passes.GetEnumerator();
            ShaderPassesClouds = MSTSSkyShader.Techniques["Clouds"].Passes.GetEnumerator();

            MSTSSkyShader.SkyMapTexture = MSTSSkyTexture;
            MSTSSkyShader.StarMapTexture = MSTSSkyStarTextureN;
            MSTSSkyShader.MoonMapTexture = MSTSSkyMoonTexture;
            MSTSSkyShader.MoonMaskTexture = MSTSSkyMoonMask;
            MSTSSkyShader.CloudMapTexture = MSTSSkyCloudTexture;
        }
        public override void Render(GraphicsDevice graphicsDevice, IEnumerable<RenderItem> renderItems, ref Matrix XNAViewMatrix, ref Matrix XNAProjectionMatrix)
        {
            // Adjust Fog color for day-night conditions and overcast
            FogDay2Night(
                Viewer.World.MSTSSky.mstsskysolarDirection.Y,
                Viewer.World.MSTSSky.mstsskyovercastFactor);


            //if (Viewer.Settings.DistantMountains) SharedMaterialManager.FogCoeff *= (3 * (5 - Viewer.Settings.DistantMountainsFogValue) + 0.5f);

            if (Viewer.World.MSTSSky.mstsskylatitude > 0) // TODO: Use a dirty flag to determine if it is necessary to set the texture again
                MSTSSkyShader.StarMapTexture = MSTSSkyStarTextureN;
            else
                MSTSSkyShader.StarMapTexture = MSTSSkyStarTextureS;
            MSTSSkyShader.Random = Viewer.World.MSTSSky.mstsskymoonPhase; // Keep setting this before LightVector for the preshader to work correctly
            MSTSSkyShader.LightVector = Viewer.World.MSTSSky.mstsskysolarDirection;
            MSTSSkyShader.Time = (float)Viewer.Simulator.ClockTime / 100000;
            MSTSSkyShader.MoonScale = MSTSSkyConstants.skyRadius / 20;
            MSTSSkyShader.Overcast = Viewer.World.MSTSSky.mstsskyovercastFactor;
            MSTSSkyShader.SetFog(Viewer.World.MSTSSky.mstsskyfogDistance, ref SharedMaterialManager.FogColor);
            MSTSSkyShader.WindSpeed = Viewer.World.MSTSSky.mstsskywindSpeed;
            MSTSSkyShader.WindDirection = Viewer.World.MSTSSky.mstsskywindDirection; // Keep setting this after Time and Windspeed. Calculating displacement here.

            // Sky dome
            var rs = graphicsDevice.RenderState;
            rs.DepthBufferWriteEnable = false;

            MSTSSkyShader.CurrentTechnique = MSTSSkyShader.Techniques["Sky"];
            Viewer.World.MSTSSky.MSTSSkyMesh.drawIndex = 1;

            Matrix viewXNASkyProj = XNAViewMatrix * Camera.XNASkyProjection;

            MSTSSkyShader.SetViewMatrix(ref XNAViewMatrix);
            MSTSSkyShader.Begin();
            ShaderPassesSky.Reset();
            while (ShaderPassesSky.MoveNext())
            {
                ShaderPassesSky.Current.Begin();
                foreach (var item in renderItems)
                {
                    Matrix wvp = item.XNAMatrix * viewXNASkyProj;
                    MSTSSkyShader.SetMatrix(ref wvp);
                    MSTSSkyShader.CommitChanges();
                    item.RenderPrimitive.Draw(graphicsDevice);
                }
                ShaderPassesSky.Current.End();
            }
            MSTSSkyShader.End();

            // Moon
            MSTSSkyShader.CurrentTechnique = MSTSSkyShader.Techniques["Moon"];
            Viewer.World.MSTSSky.MSTSSkyMesh.drawIndex = 2;

            rs.AlphaBlendEnable = true;
            rs.CullMode = CullMode.CullClockwiseFace;
            rs.DestinationBlend = Blend.InverseSourceAlpha;
            rs.SourceBlend = Blend.SourceAlpha;

            // Send the transform matrices to the shader
            int mstsskyRadius = Viewer.World.MSTSSky.MSTSSkyMesh.mstsskyRadius;
            XNAMoonMatrix = Matrix.CreateTranslation(Viewer.World.MSTSSky.mstsskylunarDirection * (mstsskyRadius - 2));
            Matrix XNAMoonMatrixView = XNAMoonMatrix * XNAViewMatrix;

            MSTSSkyShader.Begin();
            ShaderPassesMoon.Reset();
            while (ShaderPassesMoon.MoveNext())
            {
                ShaderPassesMoon.Current.Begin();
                foreach (var item in renderItems)
                {
                    Matrix wvp = item.XNAMatrix * XNAMoonMatrixView * Camera.XNASkyProjection;
                    MSTSSkyShader.SetMatrix(ref wvp);
                    MSTSSkyShader.CommitChanges();
                    item.RenderPrimitive.Draw(graphicsDevice);
                }
                ShaderPassesMoon.Current.End();
            }
            MSTSSkyShader.End();

            // Clouds
            MSTSSkyShader.CurrentTechnique = MSTSSkyShader.Techniques["Clouds"];
            Viewer.World.MSTSSky.MSTSSkyMesh.drawIndex = 3;

            rs.CullMode = CullMode.CullCounterClockwiseFace;

            MSTSSkyShader.Begin();
            ShaderPassesClouds.Reset();
            while (ShaderPassesClouds.MoveNext())
            {
                ShaderPassesClouds.Current.Begin();
                foreach (var item in renderItems)
                {
                    Matrix wvp = item.XNAMatrix * viewXNASkyProj;
                    MSTSSkyShader.SetMatrix(ref wvp);
                    MSTSSkyShader.CommitChanges();
                    item.RenderPrimitive.Draw(graphicsDevice);
                }
                ShaderPassesClouds.Current.End();
            }
            MSTSSkyShader.End();
        }

        public override void ResetState(GraphicsDevice graphicsDevice)
        {
            var rs = graphicsDevice.RenderState;
            rs.AlphaBlendEnable = false;
            rs.DepthBufferWriteEnable = true;
            rs.DestinationBlend = Blend.Zero;
            rs.SourceBlend = Blend.One;
        }

        public override bool GetBlending()
        {
            return false;
        }

        const float nightStart = 0.15f; // The sun's Y value where it begins to get dark
        const float nightFinish = -0.05f; // The Y value where darkest fog color is reached and held steady

        // These should be user defined in the Environment files (future)
        static Vector3 startColor = new Vector3(0.647f, 0.651f, 0.655f); // Original daytime fog color - must be preserved!
        static Vector3 finishColor = new Vector3(0.05f, 0.05f, 0.05f); //Darkest nighttime fog color

        /// <summary>
        /// This function darkens the fog color as night begins to fall
        /// as well as with increasing overcast.
        /// </summary>
        /// <param name="sunHeight">The Y value of the sunlight vector</param>
        /// <param name="overcast">The amount of overcast</param>
        static void FogDay2Night(float sunHeight, float overcast)
        {
            Vector3 floatColor;

            if (sunHeight > nightStart)
                floatColor = startColor;
            else if (sunHeight < nightFinish)
                floatColor = finishColor;
            else
            {
                var amount = (sunHeight - nightFinish) / (nightStart - nightFinish);
                floatColor = Vector3.Lerp(finishColor, startColor, amount);
            }

            // Adjust fog color for overcast
            floatColor *= (1 - 0.5f * overcast);
            SharedMaterialManager.FogColor.R = (byte)(floatColor.X * 255);
            SharedMaterialManager.FogColor.G = (byte)(floatColor.Y * 255);
            SharedMaterialManager.FogColor.B = (byte)(floatColor.Z * 255);
        }
    }
    #endregion
}
