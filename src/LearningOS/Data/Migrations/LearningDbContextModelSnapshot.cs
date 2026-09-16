using System;
using LearningOS.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace LearningOS.Data.Migrations
{
    [DbContext(typeof(LearningDbContext))]
    partial class LearningDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "8.0.11");
#pragma warning restore 612, 618
        }
    }
}
