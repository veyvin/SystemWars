﻿using MonoGameEngineCore;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using MonoGameEngineCore.Helper;
using System.IO;
using MonoGameEngineCore.Procedural;
using MonoGameEngineCore.GameObject;
using MonoGameEngineCore.Rendering.Camera;
using System;
using RestSharp;
using MonoGameEngineCore.Rendering;
using MonoGameEngineCore.DoomLib;
using Kaitai;
using NickLib.Pathfinding;
using MonoGameDirectX11.Screens.Doom;
using MonoGameDirectX11.Screens.Doom.DoomLib;

namespace MonoGameDirectX11.Screens
{
    public class RestfulDoomBotTest : Screen
    {
        bool connectToServer = false;
        GameObject cameraObject;
        DoomWad doomWad;
        string filePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\Downloads\\Doom1.WAD";
        float scale = 50f;
        float offsetX = 0;
        float offsetZ = 0;
        float playerUpdateFrequency = 500;
        float worldUpdateFrequency = 3000;
        float hitVectorUpdateFrequency = 500;
        DateTime timeOfLastPlayerUpdate = DateTime.Now;
        DateTime timeOfLastWorldUpdate = DateTime.Now;
        DateTime timeOfLastHitVectorUpdate = DateTime.Now;
        DoomAPIHandler apiHandler;
        GameObject playerObj;
        string restHost = "http://192.168.1.77";
        int restPort = 6001;
        double playerAngle;
        Dictionary<int, GameObject> worldObjects;
        DoomWad.Linedefs lineDefs;
        DoomWad.Things things;
        DoomWad.Sectors sectors;
        DoomWad.Blockmap blockMap;
        DoomWad.Vertexes vertices;
        DoomWad.Sidedefs sideDefs;
        private List<DoomLine> levelLines;
        private LibTessDotNet.Double.Tess tesselator;
        private AStar aStar;
        private Vector3 mouseLeftPoint, mouseRightPoint;
        private List<NavigationNode> path;
        private DoomFloodFill floodFiller;



        public RestfulDoomBotTest()
        {

        }

        public override void OnInitialise()
        {
            SystemCore.CursorVisible = true;
            SystemCore.ActiveScene.SetUpBasicAmbientAndKey();

            cameraObject = new GameObject();

            //give it some random ID to keep it out the range of the doom objects
            cameraObject.ID = 990000;


            cameraObject.AddComponent(new ComponentCamera(MathHelper.PiOver4, SystemCore.GraphicsDevice.Viewport.AspectRatio, 0.25f, 1000.0f, false));
            SystemCore.GameObjectManager.AddAndInitialiseGameObject(cameraObject);
            SystemCore.SetActiveCamera(cameraObject.GetComponent<ComponentCamera>());
            cameraObject.Transform.AbsoluteTransform = Matrix.CreateWorld(new Vector3(0, -500, 0), new Vector3(0, 1, 0), new Vector3(0, 0, 1));


            var file = new FileInfo(filePath);
            using (var fs = file.OpenRead())
            {

                var doomWad = DoomWad.FromFile(filePath);

                string desiredLevel = "E1M1";

                int levelMarker = doomWad.Index.FindIndex(x => x.Name.Contains(desiredLevel));
                for (int i = levelMarker + 1; i < doomWad.NumIndexEntries; i++)
                {
                    var currentIndex = doomWad.Index[i];

                    if (currentIndex.Name.Contains("E1M2"))
                        break;

                    if (currentIndex.Name == ("SECTORS\0"))
                        sectors = currentIndex.Contents as DoomWad.Sectors;

                    if (currentIndex.Name.Contains("BLOCKMAP"))
                        blockMap = currentIndex.Contents as DoomWad.Blockmap;

                    if (currentIndex.Name.Contains("SIDEDEF"))
                        sideDefs = currentIndex.Contents as DoomWad.Sidedefs;

                    if (currentIndex.Name.Contains("VERTEX"))
                        vertices = currentIndex.Contents as DoomWad.Vertexes;

                    if (currentIndex.Name.Contains("THING"))
                        things = currentIndex.Contents as DoomWad.Things;

                    if (currentIndex.Name.Contains("LINEDEF"))
                        lineDefs = currentIndex.Contents as DoomWad.Linedefs;
                }
            }


            worldObjects = new Dictionary<int, GameObject>();
            apiHandler = new DoomAPIHandler(restHost, restPort);
            aStar = new AStar();

            GenerateNavStructures();

            base.OnInitialise();
        }

        private void GenerateNavStructures()
        {
            levelLines = new List<DoomLine>();
            foreach (DoomWad.Linedef lineDef in lineDefs.Entries)
            {
                //sector tag is useless - always 0
                //DoomWad.Sector sector = sectors.Entries[lineDef.SectorTag];

                DoomWad.Sidedef sideDefLeft, sideDefRight;
                sideDefLeft = null;

                //most lineDefs are not double sided. SideDef ID of 65535 means no side def for this direction
                if (lineDef.SidedefLeftIdx != 65535)
                {
                    sideDefLeft = sideDefs.Entries[lineDef.SidedefLeftIdx];

                }

                sideDefRight = sideDefs.Entries[lineDef.SidedefRightIdx];

                var sector = sectors.Entries[sideDefRight.SectorId];

                Color lineColor, lineColorMin, lineColorMax;


                float heightDiff = 0;
                if (sideDefLeft != null)
                {
                    var sec1 = sectors.Entries[sideDefRight.SectorId];
                    var sec2 = sectors.Entries[sideDefLeft.SectorId];
                    heightDiff = Math.Abs(sec1.FloorZ - sec2.FloorZ);
                }



                //convert the ceiling or floor height to a useful range.
                float convertedHeight = sector.CeilZ / 8;
                convertedHeight += 17;
                convertedHeight /= 34;

                lineColorMin = Color.Blue;
                lineColorMax = Color.Red;

                lineColor = Color.Lerp(lineColorMin, lineColorMax, convertedHeight);


                DoomWad.Vertex start = vertices.Entries[lineDef.VertexStartIdx];
                DoomWad.Vertex end = vertices.Entries[lineDef.VertexEndIdx];



                var p1 = new Vector3((start.X) / scale + offsetX, 0,
                                      (start.Y) / scale + offsetZ);

                var p2 = new Vector3((end.X) / scale + offsetX, 0,
                                   (end.Y) / scale + offsetZ);

                if (sideDefLeft == null)
                    levelLines.Add(new DoomLine() { start = p1, end = p2, color = lineColor });

                if (sideDefLeft != null && heightDiff > 24)
                    levelLines.Add(new DoomLine() { start = p1, end = p2, color = lineColor });

            }

            //TesselateLevel();

            FloodFillLevel();



        }

        private Vector3 ConvertPosition(float x, float y)
        {
            return new Vector3(x / scale + offsetX, 0, y / scale + offsetZ);
        }

        private void FloodFillLevel()
        {
            var playerStartThing = things.Entries.Find(x => x.Type == 1);

            Vector3 pos = ConvertPosition(playerStartThing.X, playerStartThing.Y);


            floodFiller = new DoomFloodFill(null, levelLines, pos, vertices, scale, offsetX, offsetZ);

        }

        private void TesselateLevel()
        {
            tesselator = new LibTessDotNet.Double.Tess();
            var contour = new LibTessDotNet.Double.ContourVertex[levelLines.Count];
            for (int i = 0; i < levelLines.Count; i++)
            {
                contour[i].Position = new LibTessDotNet.Double.Vec3() { X = levelLines[i].start.X, Y = levelLines[i].start.Z, Z = 0 };
            }

            tesselator.AddContour(contour, LibTessDotNet.Double.ContourOrientation.Clockwise);

            tesselator.Tessellate(LibTessDotNet.Double.WindingRule.EvenOdd, LibTessDotNet.Double.ElementType.Polygons, 3);
        }

        private void ResponseWorldObjects(IRestResponse response)
        {

            foreach (GameObject worldObject in worldObjects.Values)
                SystemCore.GameObjectManager.RemoveObject(worldObject);

            worldObjects.Clear();

            IDictionary<string, object> jsonValues = Json.JsonParser.FromJson(response.Content);
            UpdateWorldObjects(jsonValues);

        }

        private void ResponsePlayerDetails(IRestResponse response)
        {
            IDictionary<string, object> jsonValues = Json.JsonParser.FromJson(response.Content);
            UpdatePlayer(jsonValues);

        }

        int requestVec = 0;
        private void RequestHitVectorData()
        {
            if (playerObj == null)
                return;


            DoomComponent playerDoomComponent = playerObj.GetComponent<DoomComponent>();

            Vector3 forwardVec = playerObj.Transform.AbsoluteTransform.Translation
                + playerObj.Transform.AbsoluteTransform.Forward * playerDoomComponent.HitVectorSize;

            Vector3 rightVec = MonoMathHelper.RotateAroundPoint(forwardVec, playerObj.Transform.AbsoluteTransform.Translation, Vector3.Up, MathHelper.PiOver4);
            Vector3 leftVec = MonoMathHelper.RotateAroundPoint(forwardVec, playerObj.Transform.AbsoluteTransform.Translation, Vector3.Up, -MathHelper.PiOver4);


            forwardVec *= scale;
            leftVec *= scale;
            rightVec *= scale;


            string paramString = "id=" + playerObj.ID + "&x=" + forwardVec.X + "&y=" + forwardVec.Z;
            string paramStringLeft = "id=" + playerObj.ID + "&x=" + leftVec.X + "&y=" + leftVec.Z;
            string paramStringRight = "id=" + playerObj.ID + "&x=" + rightVec.X + "&y=" + rightVec.Z;

            if (requestVec == 0)
            {
                apiHandler.EnqueueRequest(false, "leftHitVec", new RestRequest("world/movetest?" + paramStringLeft));
                requestVec++;
            }
            if (requestVec == 1)
            {
                apiHandler.EnqueueRequest(false, "rightHitVec", new RestRequest("world/movetest?" + paramStringRight));
                requestVec++;
            }
            if (requestVec == 2)
            {
                apiHandler.EnqueueRequest(false, "forwardHitVec", new RestRequest("world/movetest?" + paramString));
                requestVec = 0;
                timeOfLastHitVectorUpdate = DateTime.Now;
            }


        }

        private void ResponseHitVector(RestResponse response, string vec)
        {

            DoomComponent playerDoomComponent = playerObj.GetComponent<DoomComponent>();

            if (vec == "leftHitVec")
            {
                var content = Json.JsonParser.FromJson(response.Content);
                playerDoomComponent.LeftHitVector = (bool)content["result"];
            }
            if (vec == "forwardHitVec")
            {
                var content = Json.JsonParser.FromJson(response.Content);
                playerDoomComponent.ForwardHitVector = (bool)content["result"];
            }
            if (vec == "rightHitVec")
            {
                var content = Json.JsonParser.FromJson(response.Content);
                playerDoomComponent.RightHightVector = (bool)content["result"];
            }

        }

        private void UpdateWorldObjects(IDictionary<string, object> jsonValues)
        {
            List<object> objectList = jsonValues["array0"] as List<object>;

            foreach (object o in objectList)
            {
                double id, angle, health, distance, x, y;
                string type;

                ParseObjectData(o, out id, out type, out angle, out health, out distance, out x, out y);

                if (id == 0)
                    continue;

                GameObject worldObject;
                DoomComponent component;

                CreateLocalWorldObject(id, type, out worldObject, out component);

                UpdateObject(worldObject, angle, x, y, type, health, distance);


            }


        }

        private void CreateLocalWorldObject(double id, string type, out GameObject worldObject, out DoomComponent component)
        {
            var shape = CreateShape(type);

            worldObject = GameObjectFactory.CreateRenderableGameObjectFromShape(
                        shape, EffectLoader.LoadSM5Effect("flatshaded"));

            component = new DoomComponent();
            worldObject.AddComponent(component);
            worldObject.ID = (int)id;
            SystemCore.GameObjectManager.AddAndInitialiseGameObject(worldObject);
            worldObjects.Add(worldObject.ID, worldObject);
        }

        private ProceduralShape CreateShape(string type)
        {
            return new ProceduralCube();
        }

        private static void ParseObjectData(object o, out double id, out string type, out double angle, out double health, out double distance, out double x, out double y)
        {
            Dictionary<string, object> properties = o as Dictionary<string, object>;

            id = (double)properties["id"];
            type = (string)properties["type"];
            angle = (double)properties["angle"];
            health = (double)properties["health"];
            distance = (double)properties["distance"];

            IDictionary<string, object> pos = properties["position"] as IDictionary<string, object>;
            x = (double)pos["x"];
            y = (double)pos["y"];
        }

        private void UpdateObject(GameObject objectToUpdate, double angle, double x, double y, string type, double health, double distance)
        {
            //position the object in the world
            objectToUpdate.Transform.SetPosition(new Vector3((float)x / scale, 0, (float)y / scale));

            //turn to face the appopriate angle
            float angleInRadians = MathHelper.ToRadians((float)angle);
            Vector3 headingVector = new Vector3((float)Math.Cos(angleInRadians), 0, (float)Math.Sin(angleInRadians));
            objectToUpdate.Transform.SetLookAndUp(headingVector, Vector3.Up);

            DoomComponent component = objectToUpdate.GetComponent<DoomComponent>();
            component.DoomType = type;
            component.Health = health;
            component.Angle = angle;
            component.Distance = distance;

        }

        private void UpdatePlayer(IDictionary<string, object> jsonValues)
        {
            //in doom, 0 degrees is East. Increased value turns us counter clockwise, so north is 90, west 180 etc
            playerAngle = (double)jsonValues["angle"];



            IDictionary<string, object> pos = jsonValues["position"] as IDictionary<string, object>;
            double x = (double)pos["x"];
            double y = (double)pos["y"];

            //create if first time
            if (playerObj == null)
            {
                playerObj = GameObjectFactory.CreateRenderableGameObjectFromShape(
                        new ProceduralCube(), EffectLoader.LoadSM5Effect("flatshaded"));

                var id = (double)jsonValues["id"];
                playerObj.ID = (int)id;
                DoomComponent component = new DoomComponent();
                component.DoomType = "Player";
                component.HitVectorSize = 2f;
                playerObj.AddComponent(component);


                SystemCore.GameObjectManager.AddAndInitialiseGameObject(playerObj);

            }

            //remember to scale it appropriately. In doom, Z is up, but here it's Y, so swap those coords
            playerObj.Transform.SetPosition(new Vector3((float)x / scale, 0, (float)y / scale));

            //turn us to face the appopriate angle
            float playerAngleInRadians = MathHelper.ToRadians((float)playerAngle);
            Vector3 headingVector = new Vector3((float)Math.Cos(playerAngleInRadians), 0, (float)Math.Sin(playerAngleInRadians));

            playerObj.Transform.SetLookAndUp(headingVector, Vector3.Up);


        }

        public override void OnRemove()
        {
            base.OnRemove();
        }

        public override void Update(GameTime gameTime)
        {
            if (input.KeyPress(Microsoft.Xna.Framework.Input.Keys.Escape))
                SystemCore.ScreenManager.AddAndSetActive(new MainMenuScreen());

            float currentHeight = cameraObject.Transform.AbsoluteTransform.Translation.Y;
            cameraObject.Transform.Translate(new Vector3(0, input.ScrollDelta / 10f, 0));

            float cameraSpeed = 1f;

            if (input.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Up))
                cameraObject.Transform.Translate(new Vector3(0, 0, cameraSpeed));
            if (input.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Down))
                cameraObject.Transform.Translate(new Vector3(0, 0, -cameraSpeed));
            if (input.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Left))
                cameraObject.Transform.Translate(new Vector3(-cameraSpeed, 0, 0));
            if (input.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Right))
                cameraObject.Transform.Translate(new Vector3(cameraSpeed, 0, 0));


            if (input.KeyPress(Microsoft.Xna.Framework.Input.Keys.Space))
                floodFiller.Step();

            if (input.KeyPress(Microsoft.Xna.Framework.Input.Keys.Enter))
                floodFiller.pause = !floodFiller.pause;

            if (input.MouseLeftPress())
            {

                Plane p = new Plane(Vector3.Down, 0);
                Ray ray = input.GetProjectedMouseRay();
                float? result;
                ray.Intersects(ref p, out result);
                if (result.HasValue)
                {
                    mouseLeftPoint = ray.Position + ray.Direction * result.Value;
                    FindPath();

                }
            }
            if (input.MouseRightPress())
            {

                Plane p = new Plane(Vector3.Down, 0);
                Ray ray = input.GetProjectedMouseRay();
                float? result;
                ray.Intersects(ref p, out result);
                if (result.HasValue)
                {
                    mouseRightPoint = ray.Position + ray.Direction * result.Value;
                    FindPath();

                }
            }


            floodFiller.Update();



            //apiHandler.MovePlayerForward(10);

            if (connectToServer)
            {

                TimeSpan timeSincePlayerUpdate = DateTime.Now - timeOfLastPlayerUpdate;
                TimeSpan timeSinceWorldUpdate = DateTime.Now - timeOfLastWorldUpdate;
                TimeSpan timeSinceHitVectorUpdate = DateTime.Now - timeOfLastHitVectorUpdate;

                if (timeSincePlayerUpdate.TotalMilliseconds > playerUpdateFrequency)
                {
                    timeOfLastPlayerUpdate = DateTime.Now;
                    apiHandler.RequestPlayerDetails();
                }

                if (timeSinceWorldUpdate.TotalMilliseconds > worldUpdateFrequency)
                {
                    timeOfLastWorldUpdate = DateTime.Now;
                    apiHandler.RequestAllWorldDetails();
                }

                if (timeSinceHitVectorUpdate.TotalMilliseconds > hitVectorUpdateFrequency)
                    RequestHitVectorData();

                apiHandler.Update();

                HandleResponses();
            }

            base.Update(gameTime);
        }

        private void FindPath()
        {
            NavigationNode a = floodFiller.FindNearestPoint(mouseLeftPoint);
            NavigationNode b = floodFiller.FindNearestPoint(mouseRightPoint);

            if (a == null || b == null)
                return;


            bool success;
            path = aStar.FindPath(a, b, out success);
        }

        private void HandleResponses()
        {

            var response = apiHandler.GetNextResponse();

            if (response != null)
            {

                RestRequest originalRequest = response.Request as RestRequest;
                string requestString = originalRequest.UserState as string;

                if (requestString == "worldObjects")
                    ResponseWorldObjects(response);

                if (requestString == "playerDetails")
                    ResponsePlayerDetails(response);

                if (requestString.Contains("HitVec"))
                    ResponseHitVector(response, requestString);

            }
        }

        public override void Render(GameTime gameTime)
        {
            SystemCore.GraphicsDevice.Clear(Color.Black);
            DebugShapeRenderer.VisualiseAxes(5f);

            RenderLineDefs();

            RenderFloodFill();

            RenderPath();

            //RenderPartition();

            if (playerObj != null)
            {
                DoomComponent playerDoomComponent = playerObj.GetComponent<DoomComponent>();

                Color forwardColor = Color.Red;
                Color leftColor = Color.Red;
                Color rightColor = Color.Red;

                if (playerDoomComponent.ForwardHitVector)
                    forwardColor = Color.Blue;
                if (playerDoomComponent.LeftHitVector)
                    leftColor = Color.Blue;
                if (playerDoomComponent.RightHightVector)
                    rightColor = Color.Blue;


                Vector3 pos = playerObj.Transform.AbsoluteTransform.Translation;
                Vector3 forwardVec = playerObj.Transform.AbsoluteTransform.Translation + playerObj.Transform.AbsoluteTransform.Forward * playerDoomComponent.HitVectorSize;
                Vector3 rightVec = MonoMathHelper.RotateAroundPoint(forwardVec, playerObj.Transform.AbsoluteTransform.Translation, Vector3.Up, MathHelper.PiOver4);
                Vector3 leftVec = MonoMathHelper.RotateAroundPoint(forwardVec, playerObj.Transform.AbsoluteTransform.Translation, Vector3.Up, -MathHelper.PiOver4);

                DebugShapeRenderer.AddLine(pos, forwardVec, forwardColor);
                DebugShapeRenderer.AddLine(pos, leftVec, leftColor);
                DebugShapeRenderer.AddLine(pos, rightVec, rightColor);



                DebugText.Write(playerObj.Transform.AbsoluteTransform.Translation.ToString());
                DebugText.Write(playerAngle.ToString());
            }



            base.Render(gameTime);
        }

        private void RenderPartition()
        {
            foreach (Bucket b in floodFiller.partition.buckets)
            {

                DebugShapeRenderer.AddLine(b.bottomLeft, b.topLeft, Color.Aqua);
                DebugShapeRenderer.AddLine(b.topLeft, b.topRight, Color.Aqua);
                DebugShapeRenderer.AddLine(b.topRight, b.bottomRight, Color.Aqua);
                DebugShapeRenderer.AddLine(b.bottomRight, b.bottomLeft, Color.Aqua);

            }
        }

        private void RenderPath()
        {
            if (path == null)
                return;
            if (path.Count == 0)
                return;

            for (int i = 0; i < path.Count - 1; i++)
            {
                DebugShapeRenderer.AddLine(path[i].WorldPosition, path[i + 1].WorldPosition, Color.Purple);

            }
        }

        private void RenderFloodFill()
        {
            foreach (NavigationNode vec in floodFiller.positions)
            {
                Color colorOfSphere = Color.Orange;

                if (vec == floodFiller.next)
                    colorOfSphere = Color.Blue;

                if (floodFiller.justAdded.Contains(vec))
                    colorOfSphere = Color.Red;

                if (!vec.done)
                    DebugShapeRenderer.AddBoundingSphere(new BoundingSphere(vec.WorldPosition, 0.2f), colorOfSphere);

                foreach (NavigationNode neighbour in vec.Neighbours)
                {
                    DebugShapeRenderer.AddLine(vec.WorldPosition, neighbour.WorldPosition, Color.Green);
                }
            }


        }

        private void RenderLineDefs()
        {


            foreach (DoomLine l in levelLines)
            {

                DebugShapeRenderer.AddLine(l.start, l.end, l.color);

            }

            //int numTriangles = tesselator.ElementCount;

            //for(int i = 0; i < numTriangles; i++)
            //{
            //    var v0 = tesselator.Vertices[tesselator.Elements[i * 3]].Position;
            //    var v1 = tesselator.Vertices[tesselator.Elements[i * 3 + 1]].Position;
            //    var v2 = tesselator.Vertices[tesselator.Elements[i * 3 + 2]].Position;

            //    DebugShapeRenderer.AddLine(new Vector3((float)v0.X,0,(float)v0.Y), new Vector3((float)v1.X, 0, (float)v1.Y),Color.Orange);
            //    DebugShapeRenderer.AddLine(new Vector3((float)v1.X, 0, (float)v1.Y), new Vector3((float)v2.X, 0, (float)v2.Y), Color.Orange);
            //    DebugShapeRenderer.AddLine(new Vector3((float)v2.X, 0, (float)v2.Y), new Vector3((float)v0.X, 0, (float)v0.Y), Color.Orange);

            //}

        }
    }





}
