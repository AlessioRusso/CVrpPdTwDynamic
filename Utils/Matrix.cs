using BidirectionalMap;
using CVrpPdTwDynamic.Models;


namespace CVrpPdTwDynamic.Utils

{
    public class Matrix
    {
        public static long[,] ComputeEuclideanCostMatrix(in long[,] locations)
        {
            // Calculate the distance matrix using Euclidean distance.
            int locationNumber = locations.GetLength(0);
            long[,] distanceMatrix = new long[locationNumber, locationNumber];
            for (int fromNode = 0; fromNode < locationNumber; fromNode++)
            {
                for (int toNode = 0; toNode < locationNumber; toNode++)
                {
                    if (fromNode == toNode)
                        distanceMatrix[fromNode, toNode] = 0;
                    else
                        distanceMatrix[fromNode, toNode] =
                            (long)Math.Sqrt(Math.Pow(locations[toNode, 0] - locations[fromNode, 0], 2) +
                                            Math.Pow(locations[toNode, 1] - locations[fromNode, 1], 2));
                }
            }
            return distanceMatrix;
        }


        public static long[,] StartRiderCost(in BiMap<string, int> map,
                                             in List<List<Tuple<string, long, long>>> solution_map,
                                             in long[,] costMatrix,
                                             in List<int> present
                                             )
        {
            for (int i = 0; i < solution_map.Count; i++)
            {
                if (present[i] != 0 && present[i] < solution_map[i].Count - 1)
                {
                    var node = solution_map[i][present[i]].Item1;
                    for (int j = 0; j < costMatrix.GetLength(1); ++j)
                    {
                        if (node.Equals(map.Reverse[j]) == false)
                            costMatrix[map.Forward[solution_map[i][0].Item1], j] += DataModel.Infinite;
                    }
                }
            }

            return costMatrix;
        }
    }
}