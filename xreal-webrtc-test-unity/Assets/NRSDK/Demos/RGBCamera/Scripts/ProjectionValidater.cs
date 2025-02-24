using UnityEngine;
using System.IO;
using System.Collections.Generic;
using NRKernal;
using System;

public class ProjectionValidater
{
    // File paths (you can update these paths as needed)
    private string projectCsvPath;
    private string unprojectCsvPath;
    private string resultsCsvPath;

    public Func<NativeVector3f, NativeVector2f> ProjectPointFunc;
    public Func<NativeVector2f, NativeVector3f> UnProjectPointFunc;

    public void StartValidate(string projectFile, string unprojectFile, string resultFile)
    {
        projectCsvPath = $"{Application.persistentDataPath}/output/{projectFile}";
        unprojectCsvPath = $"{Application.persistentDataPath}/output/{unprojectFile}";
        resultsCsvPath = $"{Application.persistentDataPath}/output/{resultFile}";
        TestProjectPoint();
        TestUnProjectPoint();
    }

    private void TestProjectPoint()
    {
        Debug.Log($"Testing ProjectPoint: {projectCsvPath}");
        List<ProjectTestData> testDataList = ReadProjectCsv(projectCsvPath);
        using (StreamWriter sw = new StreamWriter(resultsCsvPath, true)) // Append mode
        {
            sw.WriteLine("TestType,InputWorldX,InputWorldY,InputWorldZ,ExpectedPixX,ExpectedPixY,ActualPixX,ActualPixY,Result");

            foreach (var data in testDataList)
            {
                NativeVector3f worldPoint = new NativeVector3f { X = data.InputWorldX, Y = data.InputWorldY, Z = data.InputWorldZ };
                NativeVector2f projectedPoint = ProjectPointFunc(worldPoint);

                bool isSuccess = Mathf.Approximately(projectedPoint.X, data.OutputPixX) && Mathf.Approximately(projectedPoint.Y, data.OutputPixY);
                string result = isSuccess ? "Pass" : "Fail";

                Debug.Log($"Testing ProjectPoint: WorldPoint({data.InputWorldX}, {data.InputWorldY}, {data.InputWorldZ})");

                // Log results to CSV
                sw.WriteLine($"ProjectPoint,{data.InputWorldX},{data.InputWorldY},{data.InputWorldZ},{data.OutputPixX},{data.OutputPixY},{projectedPoint.X},{projectedPoint.Y},{result}");
            }
        }
    }

    private void TestUnProjectPoint()
    {
        Debug.Log($"Testing UnProjectPoint: {unprojectCsvPath}");
        List<UnProjectTestData> testDataList = ReadUnprojectCsv(unprojectCsvPath);
        using (StreamWriter sw = new StreamWriter(resultsCsvPath, true)) // Append mode
        {
            sw.WriteLine("TestType,InputPixX,InputPixY,ExpectedWorldX,ExpectedWorldY,ExpectedWorldZ,ActualWorldX,ActualWorldY,ActualWorldZ,Result");

            foreach (var data in testDataList)
            {
                NativeVector2f imagePoint = new NativeVector2f { X = data.InputPixX, Y = data.InputPixY };
                NativeVector3f unprojectedPoint = UnProjectPointFunc(imagePoint);

                bool isSuccess = Mathf.Approximately(unprojectedPoint.X, data.OutputWorldX) &&
                                 Mathf.Approximately(unprojectedPoint.Y, data.OutputWorldY) &&
                                 Mathf.Approximately(unprojectedPoint.Z, data.OutputWorldZ);
                string result = isSuccess ? "Pass" : "Fail";

                Debug.Log($"Testing UnProjectPoint: ImagePoint({data.InputPixX}, {data.InputPixY})");

                // Log results to CSV
                sw.WriteLine($"UnProjectPoint,{data.InputPixX},{data.InputPixY},{data.OutputWorldX},{data.OutputWorldY},{data.OutputWorldZ},{unprojectedPoint.X},{unprojectedPoint.Y},{unprojectedPoint.Z},{result}");
            }
        }
    }

    private List<ProjectTestData> ReadProjectCsv(string path)
    {
        List<ProjectTestData> dataList = new List<ProjectTestData>();

        using (StreamReader sr = new StreamReader(path))
        {
            sr.ReadLine(); // Skip header
            while (!sr.EndOfStream)
            {
                string[] line = sr.ReadLine().Split(',');
                ProjectTestData data = new ProjectTestData
                {
                    InputWorldX = float.Parse(line[0]),
                    InputWorldY = float.Parse(line[1]),
                    InputWorldZ = float.Parse(line[2]),
                    OutputPixX = float.Parse(line[3]),
                    OutputPixY = float.Parse(line[4])
                };
                dataList.Add(data);
            }
        }

        return dataList;
    }

    private List<UnProjectTestData> ReadUnprojectCsv(string path)
    {
        List<UnProjectTestData> dataList = new List<UnProjectTestData>();

        using (StreamReader sr = new StreamReader(path))
        {
            sr.ReadLine(); // Skip header
            while (!sr.EndOfStream)
            {
                string[] line = sr.ReadLine().Split(',');
                UnProjectTestData data = new UnProjectTestData
                {
                    InputPixX = float.Parse(line[0]),
                    InputPixY = float.Parse(line[1]),
                    OutputWorldX = float.Parse(line[2]),
                    OutputWorldY = float.Parse(line[3]),
                    OutputWorldZ = float.Parse(line[4])
                };
                dataList.Add(data);
            }
        }

        return dataList;
    }

    private class ProjectTestData
    {
        public float InputWorldX;
        public float InputWorldY;
        public float InputWorldZ;
        public float OutputPixX;
        public float OutputPixY;
    }

    private class UnProjectTestData
    {
        public float InputPixX;
        public float InputPixY;
        public float OutputWorldX;
        public float OutputWorldY;
        public float OutputWorldZ;
    }
}
