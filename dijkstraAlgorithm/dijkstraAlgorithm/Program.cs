using RestSharp;
using RestSharp.Deserializers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Castle.MicroKernel.Registration;
using System.Runtime.Serialization.Formatters.Binary;
using System.Diagnostics;

namespace projeBurcuYaman
{
    public class Dic
    {
        public int DicId { get; set; }
        public double DicResult { get; set; }
    }
    public class Adjacent
    {
        public Node Node1 { get; set; }
        public Node Node2 { get; set; }
        public double Weight { get; set; }
    }
    public class Node
    {
        public Node()
        {
            AdjList = new List<Adjacent>();
            DijkstraResult = double.MaxValue;
        }
        public int Id { get; set; }
        public string Label { get; set; }
        public int NodeTekrar { get; set; }
        public bool Exit { get; set; }
        public List<Adjacent> AdjList { get; set; }
        public double DijkstraResult { get; set; }
        public Node DijkstraParentNode { get; set; }
    }

    class Program
    {

        static void Main(string[] args)
        {
            StreamReader okuNode;
            okuNode = File.OpenText(@"D:\dataNode3.txt");
            string yaziNode = okuNode.ReadToEnd().Trim();
            string ifadeNode = yaziNode;
            string[] ayiraclarNode = new string[] { "node" };
            string[] parcalarNode = ifadeNode.Split(ayiraclarNode, StringSplitOptions.RemoveEmptyEntries);
            List<Node> nodeList = new List<Node>();
            for (int i = 0; i < parcalarNode.Length; i++)
            {
                string ifadeNode2 = parcalarNode[i].Trim();
                string[] ayiraclarNode2 = new string[] { "[", "]", "\r\n" };
                string[] parcalarNode2 = ifadeNode2.Split(ayiraclarNode2, StringSplitOptions.RemoveEmptyEntries);
                Node nodeData = new Node();
                nodeData.Id = Convert.ToInt32(parcalarNode2[0].Replace("id", "").Trim());
                nodeData.Label = parcalarNode2[1].Replace("label", "").Trim();
                nodeList.Add(nodeData);
            }
            StreamReader okuEdge;
            okuEdge = File.OpenText(@"D:\dataEdge3.txt");
            string yaziEdge = okuEdge.ReadToEnd().Trim();
            string ifadeEdge = yaziEdge;
            string[] ayiraclarEdge = new string[] { "edge" };
            string[] parcalarEdge = ifadeEdge.Split(ayiraclarEdge, StringSplitOptions.RemoveEmptyEntries);
            string[] ayiraclarEdge2 = new string[] { "[", "]", "\r\n" };
            var nodeDic = nodeList.ToDictionary(x => x.Id, x => x);
            Parallel.For(0, parcalarEdge.Length, i =>
            {
                string ifadeEdge2 = parcalarEdge[i].Trim();
                string[] parcalarEdge2 = ifadeEdge2.Split(ayiraclarEdge2, StringSplitOptions.RemoveEmptyEntries);
                var node1 = nodeDic[Convert.ToInt32(parcalarEdge2[0].Replace("source", "").Trim())];
                var node2 = nodeDic[Convert.ToInt32(parcalarEdge2[1].Replace("target", "").Trim())];
                var weight = Convert.ToDouble(parcalarEdge2[2].Replace("value", "").Trim());
                lock (node1)
                    node1.AdjList.Add(new Adjacent
                    {
                        Node1 = node1,
                        Node2 = node2,
                        Weight = weight,
                    });
                lock (node2)
                    node2.AdjList.Add(new Adjacent
                    {
                        Node1 = node2,
                        Node2 = node1,
                        Weight = weight,
                    });
            });
            int countExit = nodeList.Count / 10;
            foreach (var item in nodeList)
            {
                item.NodeTekrar = item.AdjList.Count;
            }
            List<Node> list_node_ordered = new List<Node>(nodeList.OrderByDescending(i => i.NodeTekrar));
            list_node_ordered.Take(countExit).ToList().ForEach(x => x.Exit = true);
            var dicNodeResult = new Dictionary<int, List<Dic>>();
            foreach (var item in nodeList.Where(x => x.Exit))
            {
                var a = item.Id;
                var startNode = nodeList.First(x => x.Id == a);
                var travelledNodes = new Dictionary<int, Node>();
                startNode.DijkstraResult = 0.0;
                List<Node> res = new List<Node>();
                // GetFullConnectedGraph(startNode, res);
                travelledNodes.Add(startNode.Id, startNode);
                DijkstraSelectNode(startNode, travelledNodes);
                //while (travelledNodes.Count != res.Count)
                while (travelledNodes.Count != nodeList.Count)
                {
                    int index = startNode.Id;
                    var tNodes = travelledNodes.Values.ToList();
                    for (int i = 0; i < tNodes.Count; i++)
                    {
                        DijkstraSelectNode(tNodes[i], travelledNodes);
                        index = tNodes[i].Id;
                    }
                }
                List<Dic> DicList = new List<Dic>();
                foreach (var itemT in travelledNodes)
                {
                    Dic d = new Dic();
                    d.DicId = itemT.Key;
                    d.DicResult = itemT.Value.DijkstraResult;
                    DicList.Add(d);
                }
                dicNodeResult.Add(startNode.Id, DicList);
            }
            foreach (var itemDicNodeResult in dicNodeResult)
            {
                for (int i = itemDicNodeResult.Value.Count - 1; i >= 0; i--)
                {
                    var id = itemDicNodeResult.Value[i].DicId;
                    var result = itemDicNodeResult.Value[i].DicResult;
                    foreach (var itemDic in dicNodeResult.Where(x => x.Key != itemDicNodeResult.Key))
                    {
                        var item = itemDic.Value.FirstOrDefault(x => x.DicId == id);
                        if (item?.DicResult > result)
                        {
                            itemDic.Value.Remove(item);
                            break;
                        }
                        else if (item?.DicResult < result)
                        {
                            itemDicNodeResult.Value.RemoveAt(i);
                            break;
                        }
                    }
                }
            }
        }
        public static void GetFullConnectedGraph(Node startNode, List<Node> res)
        {
            res.Add(startNode);

            foreach (var nNode in startNode.AdjList)
            {
                if (res.AsParallel().All(x => x.Id != nNode.Node2.Id))
                {
                    GetFullConnectedGraph(nNode.Node2, res);
                }
            }
        }
        public static void DijkstraSelectNode(Node startNode, Dictionary<int, Node> travelledNodes)
        {
            foreach (var adj in startNode.AdjList.OrderBy(x => x.Weight))
            {
                if (travelledNodes.ContainsKey(adj.Node2.Id))
                {
                    if ((startNode.DijkstraResult + adj.Weight) < travelledNodes[adj.Node2.Id].DijkstraResult)
                    {
                        travelledNodes[adj.Node2.Id].DijkstraResult = startNode.DijkstraResult + adj.Weight;
                        adj.Node2.DijkstraParentNode = startNode;
                    }
                }
                else
                {
                    adj.Node2.DijkstraResult = startNode.DijkstraResult + adj.Weight;
                    adj.Node2.DijkstraParentNode = startNode;
                    travelledNodes.Add(adj.Node2.Id, adj.Node2);
                }
            }
        }
    }
}