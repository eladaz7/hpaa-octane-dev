﻿/*
 * Certain versions of software and/or documents ("Material") accessible here may contain branding from
 * Hewlett-Packard Company (now HP Inc.) and Hewlett Packard Enterprise Company.  As of September 1, 2017,
 * the Material is now offered by Micro Focus, a separately owned and operated company.  Any reference to the HP
 * and Hewlett Packard Enterprise/HPE marks is historical in nature, and the HP and Hewlett Packard Enterprise/HPE
 * marks are the property of their respective owners.
 * __________________________________________________________________
 * MIT License
 *
 * (c) Copyright 2012-2021 Micro Focus or one of its affiliates.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated
 * documentation files (the "Software"), to deal in the Software without restriction, including without limitation
 * the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software,
 * and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all copies or
 * substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO
 * THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
 * TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 *
 * ___________________________________________________________________
 */

using System.Globalization;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace HpToolsLauncher
{
    public class JunitXmlBuilder : IXmlBuilder
    {
        private string _xmlName = "APIResults.xml";

        public string XmlName
        {
            get { return _xmlName; }
            set { _xmlName = value; }
        }
        //public const string ClassName = "uftRunner";
        public const string ClassName = "HPToolsFileSystemRunner";
        public const string RootName = "uftRunnerRoot";

        XmlSerializer _serializer = new XmlSerializer(typeof(testsuites));

        testsuites _testSuites = new testsuites();


        public JunitXmlBuilder()
        {
            _testSuites.name = RootName;
        }

        /// <summary>
        /// converts all data from the test results in to the Junit xml format and writes the xml file to disk.
        /// </summary>
        /// <param name="results"></param>
        public void CreateXmlFromRunResults(TestSuiteRunResults results)
        {
            _testSuites = new testsuites();

            testsuite uftts = new testsuite
            {
                errors = results.NumErrors.ToString(),
                tests = results.NumTests.ToString(),
                failures = results.NumFailures.ToString(),
                name = results.SuiteName,
                package = ClassName
            };
            foreach (TestRunResults testRes in results.TestRuns)
            {
                if (testRes.TestType == TestType.LoadRunner.ToString())
                {
                    testsuite lrts = CreateXmlFromLRRunResults(testRes);
                    _testSuites.AddTestsuite(lrts);
                }
                else
                {
                    testcase ufttc = CreateXmlFromUFTRunResults(testRes);
                    uftts.AddTestCase(ufttc);
                }
            }
            if (uftts.testcase.Length > 0)
            {
                _testSuites.AddTestsuite(uftts);
            }


            if (File.Exists(XmlName))
            {
                File.Delete(XmlName);
            }


            using (Stream s = File.OpenWrite(XmlName))
            {
                _serializer.Serialize(s, _testSuites);
            }
        }

        private testsuite CreateXmlFromLRRunResults(TestRunResults testRes)
        {
            testsuite lrts = new testsuite();
            int totalTests = 0, totalFailures = 0, totalErrors = 0;

            string resultFileFullPath = testRes.ReportLocation + "\\SLA.xml";
            if (File.Exists(resultFileFullPath))
            {
                try
                {
                    XmlDocument xdoc = new XmlDocument();
                    xdoc.Load(resultFileFullPath);

                    foreach (XmlNode childNode in xdoc.DocumentElement.ChildNodes)
                    {
                        if (childNode.Attributes != null && childNode.Attributes["FullName"] != null)
                        {
                            testRes.TestGroup = testRes.TestPath;
                            testcase lrtc = CreateXmlFromUFTRunResults(testRes);
                            lrtc.name = childNode.Attributes["FullName"].Value;
                            if (childNode.InnerText.ToLowerInvariant().Contains("failed"))
                            {
                                lrtc.status = "fail";
                                totalFailures++;
                            }
                            else if (childNode.InnerText.ToLowerInvariant().Contains("passed"))
                            {
                                lrtc.status = "pass";
                                lrtc.error = new error[] { };
                            }
                            totalErrors += lrtc.error.Length;
                            lrts.AddTestCase(lrtc);
                            totalTests++;
                        }
                    }
                }
                catch (System.Xml.XmlException)
                {

                }
            }

            lrts.name = testRes.TestPath;
            lrts.tests = totalTests.ToString();
            lrts.errors = totalErrors.ToString();
            lrts.failures = totalFailures.ToString();
            lrts.time = testRes.Runtime.TotalSeconds.ToString(CultureInfo.InvariantCulture);
            return lrts;
        }

        private testcase CreateXmlFromUFTRunResults(TestRunResults testRes)
        {

            testcase tc = new testcase
            {
                systemout = testRes.ConsoleOut,
                systemerr = testRes.ConsoleErr,
                report = testRes.ReportLocation,
                classname = "All-Tests." + ((testRes.TestGroup == null) ? "" : testRes.TestGroup.Replace(".", "_")),
                name = testRes.TestPath,
                type = testRes.TestType,
                time = testRes.Runtime.TotalSeconds.ToString(CultureInfo.InvariantCulture)
            };

            if (!string.IsNullOrWhiteSpace(testRes.FailureDesc))
                tc.AddFailure(new failure { message = testRes.FailureDesc });

            switch (testRes.TestState)
            {
                case TestState.Passed:
                    tc.status = "pass";
                    break;
                case TestState.Failed:
                    tc.status = "fail";
                    break;
                case TestState.Error:
                    tc.status = "error";
                    break;
                case TestState.Warning:
                    tc.status = "warning";
                    break;
                default:
                    tc.status = "pass";
                    break;
            }
            if (!string.IsNullOrWhiteSpace(testRes.ErrorDesc))
                tc.AddError(new error { message = testRes.ErrorDesc });
            return tc;
        }




    }
}
