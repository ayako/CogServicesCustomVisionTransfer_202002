using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Training;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Training.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CustomVisionTransfer202002
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                Console.WriteLine($"--- Custom Vision Transfer Tool ---\n");

                Console.Write("Type your Custom Vision Training Key and Endpoint to make copy:\n");
                Console.Write("Training Key:\n");
                var cvTrainingKey_origin = Console.ReadLine();
                Console.WriteLine($"");

                Console.Write("Endpoint:\n");
                var cvEndpoint_origin = Console.ReadLine();
                Console.WriteLine($"");

                var cvClient_origin = new CustomVisionTrainingClient(new ApiKeyServiceClientCredentials(cvTrainingKey_origin)) { Endpoint = cvEndpoint_origin };

                Console.Write("Type your Custom Vision Project Id to make copy:\n");
                var cvProjectId_origin = new Guid(Console.ReadLine());
                Console.WriteLine($"");

                //GetProject
                var project_origin = cvClient_origin.GetProject(cvProjectId_origin);
                Console.WriteLine($"Project Name: " + project_origin.Name);
                Console.WriteLine($"Project Type: " + project_origin.Settings.ClassificationType + "\n");

                //GetDomain (from DomainId for accuracy)
                var project_origin_domain = cvClient_origin.GetDomain(project_origin.Settings.DomainId);
                Console.WriteLine($"Project Domain: " + project_origin_domain.Name + "(" + project_origin_domain.Type + ")" + "\n");

                //GetIterations
                var iterations = GetIterations(cvClient_origin, cvProjectId_origin);
                Console.WriteLine($"Defined Iterations:\n");
                for (int i = 0; i < iterations.Count; i++)
                {
                    Console.WriteLine(i + ": " + iterations[i].Name + "\n");
                }
                Console.WriteLine($"Type iteration number to copy: ");
                var iteration = int.Parse(Console.ReadLine());
                Console.WriteLine($"");

                //GetTaggedImages
                var images = GetTrainedImages(cvClient_origin, cvProjectId_origin, iterations[iteration].Id);
                Console.WriteLine($"# of Trained Images : " + images.Count + "\n");

                //GetTags
                var cvTags_origin = GetTags(images);

                Console.WriteLine($"# of Trained Tags: " + cvTags_origin.Count);
                foreach (var tag in cvTags_origin)
                {
                    Console.WriteLine($" - " + tag.TagName);
                }
                Console.WriteLine($"");


                Console.Write("Type your Custom Vision Training Key and Endpoint to transfer:\n");
                Console.Write("Training Key:\n");
                var cvTrainingKey_copy = Console.ReadLine();
                Console.WriteLine($"");

                Console.Write("Endpoint:\n");
                var cvEndpoint_copy = Console.ReadLine();
                Console.WriteLine($"");

                var cvClient_copy = new CustomVisionTrainingClient(new ApiKeyServiceClientCredentials(cvTrainingKey_copy)) { Endpoint = cvEndpoint_copy };

                //CreateNewProject
                var projectName_copy = project_origin.Name + "_" + DateTime.Now.ToString("yyyyMMdd");
                var project_copy = cvClient_copy.CreateProject(
                    projectName_copy, null, project_origin.Settings.DomainId, project_origin.Settings.ClassificationType);
                Console.WriteLine($"New Project Created.");
                Console.WriteLine($"ProjectName: " + project_copy.Name);
                Console.WriteLine($"ProjectId: " + project_copy.Id + "\n");

                //CreateNewTags
                var cvTags_copy = CreateTags(cvClient_copy, project_copy.Id, cvTags_origin);

                //LoadNewTags
                Console.WriteLine($"New Tags Created.");

                //CreateImagesUrlEntities
                var imageUrlEntries = CreateImageUrlCreateEntries(project_origin_domain.Type, images, cvTags_copy);
                Console.WriteLine($"New Entries Created.\n");

                //LoadImagesUrlEntities
                LoadImageUrlEntities(cvClient_copy, project_copy.Id, imageUrlEntries);
                Console.WriteLine($"New Entries Loading Completed.\n");

                Console.WriteLine($"--- Custom Vision Transfer COMPLETED ---");

            }
            catch (Exception e)
            {

                Console.WriteLine($"Error:" + e.Message);
            }
            finally
            {
                Console.WriteLine($"Hit any key to close this console...");
                Console.ReadLine();

            }

        }

        public static IList<Iteration> GetIterations(CustomVisionTrainingClient cvClient, Guid cvProjectId)
        {
            //GetIterations
            var iterations = cvClient.GetIterations(cvProjectId);

            return iterations;
        }

        public static List<Image> GetTrainedImages(CustomVisionTrainingClient cvClient, Guid cvProjectId, Guid iterationId)
        {
            //GetTaggedImages
            List<Image> images = new List<Image>();
            var result = new List<Image>();

            var i = 0;
            do
            {
                result = cvClient.GetTaggedImages(cvProjectId, iterationId, null, null, 250, i * 250).ToList<Image>();
                if (result.Count != 0)
                {
                    images.AddRange(result);
                    i += 1;
                }
            }
            while (result.Count >= 250);

            return images;
        }

        public static List<ImageTag> GetTags(IList<Image> images)
        {
            List<ImageTag> imageTags = new List<ImageTag>();

            foreach (var image in images)
            {
                foreach (var tag in image.Tags)
                {
                    imageTags.Add(tag);
                }
            }

            imageTags = imageTags.GroupBy(x => x.TagId).Select(group => group.First()).ToList<ImageTag>();

            return imageTags;
        }


        public static List<Tag> CreateTags(CustomVisionTrainingClient cvClient, Guid projectId, List<ImageTag> tags)
        {
            List<Tag> newTags = new List<Tag>();
            foreach (var tag in tags)
            {
                newTags.Add(cvClient.CreateTag(projectId, tag.TagName));
            }
            return newTags;
        }


        public static List<ImageUrlCreateEntry> CreateImageUrlCreateEntries(string domainType, List<Image> images, List<Tag> tags)
        {
            var imageUrlCreateEntries = new List<ImageUrlCreateEntry>();

            foreach (var image in images)
            {
                var regions = new List<Region>();
                var tagIds = new List<Guid>();

                if ( domainType == DomainType.ObjectDetection )
                {
                    foreach (var region in image.Regions)
                    {
                        var tagId = tags.Where(x => x.Name == region.TagName).Select(x => x.Id).FirstOrDefault();
                        tagIds.Add(tagId);
                        regions.Add(new Region { 
                            TagId = tagId,
                            Top = region.Top,
                            Left = region.Left,
                            Height = region.Height,
                            Width = region.Width,
                        });
                    }
                }
                else
                {
                    foreach (var tag in image.Tags)
                    {
                        var tagId = tags.Where(x => x.Name == tag.TagName).Select(x => x.Id).FirstOrDefault();
                        tagIds.Add(tagId);
                    }
                }

                imageUrlCreateEntries.Add(new ImageUrlCreateEntry(){
                    Url = image.OriginalImageUri,
                    TagIds = tagIds,
                    Regions = regions,
                });
            }

            return imageUrlCreateEntries;
        }

        public static void LoadImageUrlEntities(CustomVisionTrainingClient cvClient, Guid projectId, List<ImageUrlCreateEntry> imageUrlEntries)
        {
            Console.WriteLine($"New Entries to Load: " + imageUrlEntries.Count);

            for (int i = 1; i < (double)imageUrlEntries.Count/50 + 1 ; i++)
            {
                var entries = new List<ImageUrlCreateEntry>(
                    imageUrlEntries.Skip((i-1)*50).Take(50).ToList<ImageUrlCreateEntry>()
                    );
                cvClient.CreateImagesFromUrls(projectId, new ImageUrlCreateBatch(entries));

                Console.WriteLine($"New Entries Loaded: " + (int)((i-1)*50+1) + " - " + (int)i*50);
            }
        }

    }
}
