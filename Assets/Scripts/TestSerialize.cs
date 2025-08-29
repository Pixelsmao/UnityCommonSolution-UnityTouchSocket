using Assets.Scripts;
using System;
using System.Collections.Generic;
using TouchSocket.Core;
using UnityEngine;
using UnityEngine.Assertions;

public class TestSerialize : MonoBehaviour
{
    public void ShouldSerializeObjBeOk()
    {
        try
        {
            var student = new Student();
            student.P1 = 10;
            student.P2 = "若汝棋茗";
            student.P3 = 100;
            student.P4 = 0;
            student.P5 = DateTime.Now;
            student.P6 = 10;
            student.P7 = new byte[1024 * 64];
            student.P8 = new string[] { "I", "love", "you" };

            var random = new System.Random();
            random.NextBytes(student.P7);

            student.List1 = new List<int>();
            student.List1.Add(1);
            student.List1.Add(2);
            student.List1.Add(3);

            student.List2 = new List<string>();
            student.List2.Add("1");
            student.List2.Add("2");
            student.List2.Add("3");

            student.List3 = new List<byte[]>();
            student.List3.Add(new byte[1024]);
            student.List3.Add(new byte[1024]);
            student.List3.Add(new byte[1024]);

            student.Dic1 = new Dictionary<int, int>();
            student.Dic1.Add(1, 1);
            student.Dic1.Add(2, 2);
            student.Dic1.Add(3, 3);

            student.Dic2 = new Dictionary<int, string>();
            student.Dic2.Add(1, "1");
            student.Dic2.Add(2, "2");
            student.Dic2.Add(3, "3");

            student.Dic3 = new Dictionary<string, string>();
            student.Dic3.Add("1", "1");
            student.Dic3.Add("2", "2");
            student.Dic3.Add("3", "3");

            student.Dic4 = new Dictionary<int, Arg>();
            student.Dic4.Add(1, new Arg(1));
            student.Dic4.Add(2, new Arg(2));
            student.Dic4.Add(3, new Arg(3));

            var byteBlock = new ByteBlock(1024 * 512);
            FastBinaryFormatter.Serialize(byteBlock, student);
            byteBlock.SeekToStart();
            var newStudent = FastBinaryFormatter.Deserialize<Student>(byteBlock);
            byteBlock.Dispose();

            Assert.AreEqual(student.P1, newStudent.P1);
            Assert.AreEqual(student.P2, newStudent.P2);
            Assert.AreEqual(student.P3, newStudent.P3);
            Assert.AreEqual(student.P4, newStudent.P4);
            Assert.AreEqual(student.P5, newStudent.P5);
            Assert.AreEqual(student.P6, newStudent.P6);

            Assert.IsNotNull(newStudent.P7);
            Assert.AreEqual(student.P7.Length, newStudent.P7.Length);

            Assert.IsNotNull(newStudent.P8);
            Assert.AreEqual(student.P8.Length, newStudent.P8.Length);

            for (var i = 0; i < student.P8.Length; i++)
            {
                Assert.AreEqual(student.P8[i], newStudent.P8[i]);
            }

            for (var i = 0; i < student.P7.Length; i++)
            {
                Assert.AreEqual(student.P7[i], newStudent.P7[i]);
            }

            Assert.IsNotNull(newStudent.List1);
            Assert.AreEqual(student.List1.Count, newStudent.List1.Count);
            for (var i = 0; i < student.List1.Count; i++)
            {
                Assert.AreEqual(student.List1[i], newStudent.List1[i]);
            }

            Assert.IsNotNull(newStudent.List2);
            Assert.AreEqual(student.List2.Count, newStudent.List2.Count);
            for (var i = 0; i < student.List2.Count; i++)
            {
                Assert.AreEqual(student.List2[i], newStudent.List2[i]);
            }

            Assert.IsNotNull(newStudent.List3);
            Assert.AreEqual(student.List3.Count, newStudent.List3.Count);
            for (var i = 0; i < student.List3.Count; i++)
            {
                Assert.AreEqual(student.List3[i].Length, newStudent.List3[i].Length);
            }

            Assert.IsNotNull(newStudent.Dic1);
            Assert.AreEqual(student.Dic1.Count, newStudent.Dic1.Count);

            Assert.IsNotNull(newStudent.Dic2);
            Assert.AreEqual(student.Dic2.Count, newStudent.Dic2.Count);

            Assert.IsNotNull(newStudent.Dic3);
            Assert.AreEqual(student.Dic3.Count, newStudent.Dic3.Count);

            Assert.IsNotNull(newStudent.Dic4);
            Assert.AreEqual(student.Dic4.Count, newStudent.Dic4.Count);

            UnityLog.Logger.Info("序列化完成");
        }
        catch (Exception ex)
        {
            UnityLog.Logger.Exception(ex);
        }

    }

    public void TestDpClick()
    {
        var testDpProperty = new TestDpProperty();
        testDpProperty.ClickDp();
    }
}
