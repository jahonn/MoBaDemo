﻿using BehaviorDesigner.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using UnityEngine;
using UnityEngine.UI;

public class NetWorkManager : MonoBehaviour {

    public GameObject NpcPrefab;
    public GameObject playerPrefab;

    public InputField text;
    private string nowPlayerID = "sjm";
    private bool isHomeowner = false;
    private SynchronizeTest test;

    // 单例
    private static NetWorkManager instance;
    public static NetWorkManager Instance {
        get {
            if (instance == null) {
                instance = GameObject.FindObjectOfType<NetWorkManager>();
            }
            return instance;
        }
    }

    public string NowPlayerID {
        get {
            return nowPlayerID;
        }
    }

    public bool IsHomeowner {
        get {
            return isHomeowner;
        }
    }

    private Connection connection = null;

    /// <summary>
    /// 用于管理 在该场景中的所有网络单位
    /// </summary>
    public Dictionary<string, GameObject> networkPlayers = new Dictionary<string, GameObject>();

    /// <summary>
    /// 产生一个网络对象
    /// </summary>
    /// <param name="id"></param>
    /// <param name="position"></param>
    public void AddNetworkPlayer(string id, Vector3 position) {
        GameObject player = GameObject.Instantiate(playerPrefab, position, Quaternion.identity);
        player.transform.position = new Vector3(player.transform.position.x, 0.5f, player.transform.position.z);

        // 设置CharacterMono的网络ID
        player.GetComponent<CharacterMono>().NetWorkPlayerID = id;

        if (synchronizeTest != null)
            player.GetComponent<CharacterMono>().characterModel.OnDamaged += synchronizeTest.DamageSynchronize;
        networkPlayers.Add(id, player);
    }

    public void Connect() {
        // 连接本地
        connection = new Connection("127.0.0.1", 8081);

        // 初始化监听方法
        InitProtocolListener();
    }

    public void Send(ProtocolBytes protocolBytes) {
        connection.Send(protocolBytes);
    }

    public void StartGame() {
        AddNetworkPlayer(NowPlayerID, UnityEngine.Random.insideUnitCircle * 5);
        networkPlayers[NowPlayerID].GetComponent<CharacterOperationFSM>().enabled = true;

        synchronizeTest = new SynchronizeTest(networkPlayers[NowPlayerID].GetComponent<CharacterMono>());

        SendPos();
    }

    public void SendNpcPos(string id) {
        Transform playerTransform = networkPlayers[id].transform;
        Vector3 pos = playerTransform.position;
        Vector3 rotation = playerTransform.rotation.eulerAngles;

        // 构造位置改变消息
        ProtocolBytes protocolBytes = new ProtocolBytes();
        protocolBytes.AddString("UpdateInfo");
        protocolBytes.AddString(NowPlayerID);
        protocolBytes.AddFloat(pos.x);
        protocolBytes.AddFloat(pos.y);
        protocolBytes.AddFloat(pos.z);
        protocolBytes.AddFloat(rotation.x);
        protocolBytes.AddFloat(rotation.y);
        protocolBytes.AddFloat(rotation.z);
        connection.Send(protocolBytes);
    }

    public void SendPos() {
        Transform playerTransform = networkPlayers[NowPlayerID].transform;
        Vector3 pos = playerTransform.position;
        Vector3 rotation = playerTransform.rotation.eulerAngles;

        // 构造位置改变消息
        ProtocolBytes protocolBytes = new ProtocolBytes();
        protocolBytes.AddString("UpdateInfo");
        protocolBytes.AddString(NowPlayerID);
        protocolBytes.AddFloat(pos.x);
        protocolBytes.AddFloat(pos.y);
        protocolBytes.AddFloat(pos.z);
        protocolBytes.AddFloat(rotation.x);
        protocolBytes.AddFloat(rotation.y);
        protocolBytes.AddFloat(rotation.z);
        connection.Send(protocolBytes);
    }
    
    /// <summary>
    /// 根据用户名和密码构造登录协议
    /// </summary>
    /// <param name=""></param>
    public void SendLogin(string userName,string password) {

        if (connection == null) {
            Connect();
        }

        ProtocolBytes protocolBytes = new ProtocolBytes();
        // 协议名
        protocolBytes.AddString("LoginConn");

        // 协议参数
        protocolBytes.AddString(userName);
        protocolBytes.AddString(password);

        connection.Send(protocolBytes);
    }

    public void OnLoginSuccess(string userName) {
        nowPlayerID = userName;
    }

    /// <summary>
    /// 基于Updateinfo协议，根据ID更新一个游戏单位的位置
    /// </summary>
    /// <param name="id"></param>
    /// <param name="pos"></param>
    public void UpdateInfo(string id, Vector3 pos,Vector3 rotation) {

        if (id != NowPlayerID) {
            if (!networkPlayers.ContainsKey(id)) {
                AddNetworkPlayer(id, UnityEngine.Random.insideUnitCircle * 5);
            } else {
                networkPlayers[id].transform.position = pos;
                networkPlayers[id].transform.rotation = Quaternion.Euler(rotation);
            }
        }
    }

    /// <summary>
    /// 初始化所有协议的监听方法
    /// </summary>
    public void InitProtocolListener() {
        connection.AddListener("UpdateInfo", (protocolBytes) => {
            Debug.Log("处理位置改变协议中");
            string id = protocolBytes.GetString();
            float x = protocolBytes.GetFloat();
            float y = protocolBytes.GetFloat();
            float z = protocolBytes.GetFloat();
            float tx = protocolBytes.GetFloat();
            float ty = protocolBytes.GetFloat();
            float tz = protocolBytes.GetFloat();
            Debug.Log("id：" + id + " x:" + x + " y:" + y + " z:" + z);
            UpdateInfo(id, new Vector3(x, y, z),new Vector3(tx,ty,tz));
        });
        
    }

    public void DispatchMsgEvent(ProtocolBytes protocolBytes) {
        string name = protocolBytes.GetString();
        Debug.Log("分发协议："+name);
        connection.TreateProtocol(name,protocolBytes);
    }

    /// <summary>
    /// 用于为某一个具体的协议增加监听(回调)方法
    /// </summary>
    /// <param name="name"></param>
    /// <param name="protocolHandler"></param>
    public void AddListener(string name, Connection.ProtocolHandler protocolHandler) {
        connection.AddListener(name,protocolHandler);
    }

    // Update is called once per frame
    void Update () {
        if (connection == null) return;
        while (connection.MsgList.Count > 0) {
            ProtocolBytes protocolBytes = null;
            lock (connection.MsgList) {
                protocolBytes = connection.MsgList.Dequeue();
            }
            DispatchMsgEvent(protocolBytes);
        }
	}

    #region 测试

    public void TestConnect() {
        nowPlayerID = text.text;
        Connect();
    }
    public CharacterMono characterMono;
    SynchronizeTest synchronizeTest;
    private void Start() {

    }

    #endregion
}
