using System;
using System.Collections.Generic;
using System.Linq;

namespace MVCForum.Website.Models
{
    public partial class Slide
    {
        public static List<Slide> AllForMainPage()
        {
            using (var entities = new Entities())
            {
                return entities.Slides.ToList();
            }
        }
    }
}