using System;
using System.Linq;
using DNTCaptcha.Core;
using LLB.Data;
using LLB.Models;
using LLB.Models.DataModel;
using Microsoft.AspNetCore.Identity;

namespace LLB.Helpers
{
    public class TaskAllocationHelper
    {

        


        public async Task<string> GetVerifier(AppDbContext _db, UserManager<ApplicationUser> userManager)
        {
            // running the task allocation method, to be optimised
            var verifiers = await userManager.GetUsersInRoleAsync("verifier");

            List<UserTasks> tasklist = new List<UserTasks>();
            foreach (var verifier in verifiers)
            {
                UserTasks eachsuser = new UserTasks();
                var counttasks = _db.Tasks.Where(a => a.VerifierId == verifier.Id).ToList();
                var taskcount = counttasks.Count;
                eachsuser.UserId = verifier.Id;
                eachsuser.Tasks = taskcount;
                tasklist.Add(eachsuser);

            }

            string verifierWithLeastTasks = null;
            int minTaskCount = int.MaxValue;
            foreach (var entry in tasklist)
            {
                if (entry.Tasks < minTaskCount)
                {
                    minTaskCount = entry.Tasks;
                    verifierWithLeastTasks = entry.UserId;
                }
            }

            return new string(verifierWithLeastTasks);
        }





        public  async Task<string> GetRecommender(AppDbContext _db, UserManager<ApplicationUser> userManager)
        {
            // running the task allocation method, to be optimised
            var recommenders = await userManager.GetUsersInRoleAsync("recommender");

            List<UserTasks> tasklist = new List<UserTasks>();
            foreach (var recommender in recommenders)
            {
                UserTasks eachsuser = new UserTasks();
                var counttasks = _db.Tasks.Where(a => a.RecommenderId == recommender.Id).ToList();
                var taskcount = counttasks.Count;
                eachsuser.UserId = recommender.Id;
                eachsuser.Tasks = taskcount;
                tasklist.Add(eachsuser);

            }

            string recommenderWithLeastTasks = null;
            int minTaskCount = int.MaxValue;
            foreach (var entry in tasklist)
            {
                if (entry.Tasks < minTaskCount)
                {
                    minTaskCount = entry.Tasks;
                    recommenderWithLeastTasks = entry.UserId;
                }
            }

            return new string(recommenderWithLeastTasks);
        }


        public async Task<string> GetSecretary(AppDbContext _db, UserManager<ApplicationUser> _userManager)
        {
            // running the task allocation method, to be optimised
            var secretarys = await _userManager.GetUsersInRoleAsync("secretary");

            List<UserTasks> tasklist = new List<UserTasks>();
            foreach (var secretary in secretarys)
            {
                UserTasks eachsuser = new UserTasks();
                var counttasks = _db.Tasks.Where(a => a.ApproverId == secretary.Id).ToList();
                var taskcount = counttasks.Count;
                eachsuser.UserId = secretary.Id;
                eachsuser.Tasks = taskcount;
                tasklist.Add(eachsuser);

            }

            string secretaryWithLeastTasks = null;
            int minTaskCount = int.MaxValue;
            foreach (var entry in tasklist)
            {
                if (entry.Tasks < minTaskCount)
                {
                    minTaskCount = entry.Tasks;
                    secretaryWithLeastTasks = entry.UserId;
                }
            }

            return new string(secretaryWithLeastTasks);
        }
    }
}
