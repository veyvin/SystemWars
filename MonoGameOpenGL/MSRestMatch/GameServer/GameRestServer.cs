﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;
using System.ServiceModel.Web;
using Microsoft.Xna.Framework;
using MonoGameEngineCore.GameObject;
using MonoGameEngineCore;

namespace MSRestMatch.GameServer
{
    [ServiceContract]
    public interface IService
    {


        [OperationContract, WebGet(UriTemplate = "/player/id/{id}", ResponseFormat = WebMessageFormat.Json)]
        PlayerJson GetPlayer(string id);

        [OperationContract, WebGet(UriTemplate = "/player/name/{name}", ResponseFormat = WebMessageFormat.Json)]
        PlayerJson GetPlayerByName(string name);

        [OperationContract, WebInvoke(UriTemplate = "/player/create/", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        PlayerJson CreatePlayer(PlayerCreate create);

        [OperationContract, WebInvoke(UriTemplate = "/player/action/", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        void PlayerAction(PlayerAction action);


    }

    class Service : IService
    {
        GameSimulation sim;


        public Service(GameSimulation simulation)
        {
            sim = simulation;
        }

        public PlayerJson GetPlayer(string id)
        {
            int idInt = int.Parse(id);

            var p = SystemCore.GameObjectManager.GetObject(idInt) as Player;
            if (p != null)
                return new PlayerJson() { Id = p.ID, Name = p.Name };
            else
            {
                WebOperationContext.Current.OutgoingResponse.StatusCode = System.Net.HttpStatusCode.NotFound;
                return null;
            }
        }

        public PlayerJson GetPlayerByName(string name)
        {
            var p = SystemCore.GameObjectManager.GetObject(name) as Player;
            if (p != null)
                return new PlayerJson() { Id = p.ID, Name = p.Name };
            else
            {
                WebOperationContext.Current.OutgoingResponse.StatusCode = System.Net.HttpStatusCode.NotFound;
                return null;
            }
        }

        public PlayerJson CreatePlayer(PlayerCreate create)
        {
            Player p = SystemCore.GameObjectManager.GetObject(create.Name) as Player;
            if (p != null)
            {
                WebOperationContext.Current.OutgoingResponse.StatusCode = System.Net.HttpStatusCode.BadRequest;
                return null;
            }

            sim.AddPlayer(Vector3.Zero, create.Name, Color.Red);
            p = SystemCore.GameObjectManager.GetObject(create.Name) as Player;
            return new PlayerJson() { Id = p.ID, Name = p.Name };
        }

        public void PlayerAction(PlayerAction playerAction)
        {
            Player p = SystemCore.GameObjectManager.GetObject(int.Parse(playerAction.Id)) as Player;
            if (p == null)
            {
                WebOperationContext.Current.OutgoingResponse.StatusCode = System.Net.HttpStatusCode.NotFound;
                return;
            }

            if(playerAction.Action == "set_heading")
            {
                float value = float.Parse(playerAction.Value);
                p.DesiredHeading = value;
            }

            if(playerAction.Action == "move_forward")
            {
                p.DesiredPosition = p.Transform.AbsoluteTransform.Translation + p.Transform.AbsoluteTransform.Forward * float.Parse(playerAction.Value);
            }
           
        }
    }

    public class PlayerAction
    {
        public string Id { get; set; }
        public string Action { get; set; }
        public string Value { get; set; }
    }
    public class PlayerCreate
    {
        public string Name { get; set; }
    }
    public class PlayerJson
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Heading { get; set; }
        public int Health { get; set; }
    }


}
