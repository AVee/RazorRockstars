﻿using System.Collections.Generic;
using System.Runtime.Serialization;
using Funq;
using ServiceStack.Common;
using ServiceStack.DataAnnotations;
using ServiceStack.OrmLite;
using ServiceStack.Razor;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface;
using ServiceStack.WebHost.Endpoints;

//The entire C# code for the stand-alone RazorRockstars demo.
namespace RazorRockstars.WebHost
{
    public class AppHost : AppHostBase 
    {
        public AppHost() : base("Test Razor", typeof(AppHost).Assembly) { }

        public override void Configure(Container container)
        {
            Plugins.Add(new RazorFormat());

            container.Register<IDbConnectionFactory>(
                new OrmLiteConnectionFactory(":memory:", false, SqliteDialect.Provider));

            using (var db = container.Resolve<IDbConnectionFactory>().OpenDbConnection())
            {
                db.CreateTable<Rockstar>(overwrite: false); //Create table if not exists
                db.Insert(Rockstar.SeedData); //Populate with seed data
            }
		}
    }

    public class Rockstar
    {
        public static Rockstar[] SeedData = new[] {
            new Rockstar(1, "Jimi", "Hendrix", 27), 
            new Rockstar(2, "Janis", "Joplin", 27), 
            new Rockstar(3, "Jim", "Morrisson", 27), 
            new Rockstar(4, "Kurt", "Cobain", 27),              
            new Rockstar(5, "Elvis", "Presley", 42), 
            new Rockstar(6, "Michael", "Jackson", 50), 
            new Rockstar(7, "Eddie", "Vedder", 47), 
            new Rockstar(8, "Dave", "Grohl", 43), 
            new Rockstar(9, "Courtney", "Love", 48), 
            new Rockstar(10, "Bruce", "Springsteen", 62), 
        };

        [AutoIncrement]
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public int? Age { get; set; }

        public Rockstar() { }
        public Rockstar(int id, string firstName, string lastName, int age)
        {
            Id = id;
            FirstName = firstName;
            LastName = lastName;
            Age = age;
        }
    }
    
    [RestService("/rockstars")]
    [RestService("/rockstars/aged/{Age}")]
    [RestService("/rockstars/delete/{Delete}")]
    [RestService("/rockstars/{Id}")]
    public class Rockstars
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public int? Age { get; set; }
        public string Delete { get; set; }
    }

    [DataContract] //Attrs for CSV Format to recognize it's a DTO and serialize the Enumerable property
    public class RockstarsResponse
    {
        [DataMember] public int Total { get; set; }
        [DataMember] public int? Aged { get; set; }
        [DataMember] public List<Rockstar> Results { get; set; }
    }

    public class RockstarsService : RestServiceBase<Rockstars>
    {
        public IDbConnectionFactory DbFactory { get; set; }

        public override object OnGet(Rockstars request)
        {
            using (var db = DbFactory.OpenDbConnection())
            {
                if (request.Delete == "reset")
                {
                    db.DeleteAll<Rockstar>();
                    db.Insert(Rockstar.SeedData);
                }
                else if (request.Delete.IsInt())
                {
                    db.DeleteById<Rockstar>(request.Delete.ToInt());
                }

                return new RockstarsResponse {
                    Aged = request.Age,
                    Total = db.GetScalar<int>("select count(*) from Rockstar"),
                    Results = request.Id != default(int) ?
                        db.Select<Rockstar>(q => q.Id == request.Id)
                          : request.Age.HasValue ?
                        db.Select<Rockstar>(q => q.Age == request.Age.Value)
                          : db.Select<Rockstar>()
                };
            }
        }

        public override object OnPost(Rockstars request)
        {
            using (var db = DbFactory.OpenDbConnection())
            {
                db.Insert(request.TranslateTo<Rockstar>());
                return OnGet(new Rockstars());
            }
        }
    }
}