using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Training;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Training.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CustomVisionTransfer202002
{
    class Program
    {
        private const string cvLocation = "YOUR_CV_LOCATION";
        private const string cvTrainingKey = "YOUR_CV_TRAINING_KEY";

        private const string cvEndpoint = "https://" + cvLocation + ".api.cognitive.microsoft.com/";
        static void Main(string[] args)
        {

            var cvClient = new CustomVisionTrainingClient() { Endpoint = cvEndpoint, ApiKey = cvTrainingKey };

            Console.WriteLine($"--- Custom Vision Transfer Tool ---\n");
            Console.Write("Type your Custom Vision project id to Copy:\n");
            var cvProjectId = new Guid(Console.ReadLine());
            Console.WriteLine($"");

            //GetProject
            var project = cvClient.GetProject(cvProjectId);
            Console.WriteLine($"Project Name: " + project.Name);
            Console.WriteLine($"Project Type: " + project.Settings.ClassificationType + "\n");

            //GetIterations
            var iterations = GetIterations(cvClient, cvProjectId);
            Console.WriteLine($"Latest Iteration: " + iterations[0].Name + "\n");

            //GetTaggedImages
            var images = GetTrainedImages(cvClient, cvProjectId, iterations[0].Id);
            Console.WriteLine($"# of Trained Images : " + images.Count + "\n");

            //GetTags
            var tags = GetTags(images);

            Console.WriteLine($"# of Trained Tags: " + tags.Count);
            foreach (var tag in tags)
            {
                Console.WriteLine($" - " + tag.TagName);
            }
            Console.WriteLine($"");

            //CreateNewProject
            var newProjectName = project.Name + "_" + DateTime.Now.ToString("yyyyMMdd");
            var newProject = cvClient.CreateProject(
                newProjectName, null, project.Settings.DomainId, project.Settings.ClassificationType);
            Console.WriteLine($"New Project Created.");
            Console.WriteLine($"ProjectName: " + newProject.Name);
            Console.WriteLine($"ProjectId: " + newProject.Id + "\n");

            //CreateNewTags
            var newTags = CreateTags(cvClient, newProject.Id, tags);

            //LoadNewTags
            Console.WriteLine($"New Tags Created.");

            //CreateImagesUrlEntities
            var imageUrlEntries = CreateImageUrlCreateEntries(cvClient, newProject.Settings.ClassificationType, images, newTags);
            Console.WriteLine($"New Entries Created.\n");

            //LoadImagesUrlEntities
            LoadImageUrlEntities(cvClient, newProject.Id, imageUrlEntries);
            Console.WriteLine($"New Entries Loading Completed.\n");

            Console.WriteLine($"--- Custom Vision Transfer COMPLETED ---");
            Console.ReadLine();

        }

        public static IList<Iteration> GetIterations(CustomVisionTrainingClient cvClient, Guid cvProjectId)
        {
            //GetIterations
            var iterations = cvClient.GetIterations(cvProjectId);

            return iterations;
        }

        public static IList<Image> GetTrainedImages(CustomVisionTrainingClient cvClient, Guid cvProjectId, Guid lastIterationId)
        {
            //GetTaggedImages
            var images = cvClient.GetTaggedImages(cvProjectId, lastIterationId, null, null, 256);

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


        public static List<ImageUrlCreateEntry> CreateImageUrlCreateEntries(CustomVisionTrainingClient cvClient, string classificationType, IList<Image> images, List<Tag> tags)
        {
            var imageUrlCreateEntries = new List<ImageUrlCreateEntry>();

            foreach (var image in images)
            {
                var regions = new List<Region>();
                var tagIds = new List<Guid>();

                if ( classificationType == null)
                {
                    foreach (var region in image.Regions)
                    {
                        var tagId = tags.Where(x => x.Name == region.TagName).Select(x => x.Id).FirstOrDefault();
                        tagIds.Add(tagId);
                        regions.Add(new Region { 
                            //TagId = region.TagId,
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
