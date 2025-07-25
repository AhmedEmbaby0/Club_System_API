﻿using Club_System_API.Abstractions;
using Club_System_API.Dtos.Appointment;
using Club_System_API.Dtos.Coaches;
using Club_System_API.Dtos.Service;
using Club_System_API.Dtos.ServiceReview;
using Club_System_API.Errors;
using Club_System_API.Helper;
using Club_System_API.Models;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Club_System_API.Services.service
{
    public class ServiceService : IServiceService
    {
        private readonly ApplicationDbContext _context;
        public ServiceService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Result<ServiceResponse>> AddAsync([FromForm] ServiceRequest serviceRequest, CancellationToken cancellationToken = default)
        {
            var service = serviceRequest.Adapt<Service>(); 
           await _context.AddAsync(service,cancellationToken);
           await _context.SaveChangesAsync(cancellationToken);
            return Result.Success(service.Adapt<ServiceResponse>());
        }

       

        public async Task<IEnumerable<ServiceResponse>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Services
           .AsNoTracking()
           .ProjectToType<ServiceResponse>()
           .ToListAsync(cancellationToken);
        }

        public async Task<Result<ServiceWithAllInfoResponse>> GetAsync(int id, CancellationToken cancellationToken = default)
        {
            var service = await _context.Services
                   .Include(s => s.coaches)
                   .ThenInclude(sc => sc.Coach)
                   .Include(s => s.reviews)
                   .ThenInclude(r => r.User)
                   .Include(s => s.appointments)
                   .AsNoTracking()
                   .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

            if ( service is null)
               return  Result.Failure<ServiceWithAllInfoResponse>(ServiceErrors.ServiceNotFound);

            var imageData = service.Image != null ? Convert.ToBase64String(service.Image) : null;

            var response = new ServiceWithAllInfoResponse(
                service.Id,
                service.Name,
                service.Price,
                service.Description,
                service.AverageRating,
                service.ImageContentType,
                imageData,
                service.coaches.Select(sc => sc.Coach.Adapt<CoachResponse>()).ToList(),
                service.appointments.Select(a => a.Adapt<AppointmentOfServiceResponse>()).ToList(),
                service.reviews.Select(r => new ServiceReviewWithUserImageResponse(
                    r.User.ImageContentType,
                    r.User.Image != null ? $"data:{r.User.ImageContentType};base64,{Convert.ToBase64String(r.User.Image)}": null,
                    r.User.FirstName,
                    r.User.LastName,
                    r.ReviewAt,
                    r.Rating,
                    r.Review
                ))
                .ToList()
            );

            return Result.Success(response);
        }

        public async Task<Result> UpdateAsync(int id, ServiceRequest request, CancellationToken cancellationToken = default)
        {

            var currentService = await _context.Services.FindAsync(id, cancellationToken);

            if (currentService is null)
                return Result.Failure(ServiceErrors.ServiceNotFound);

            currentService.Name = request.Name;
            currentService.Price = request.Price;
            currentService.Description = request.Description;
            currentService.Image= FormFileExtensions.ConvertToBytes(request.Image);
            currentService.ImageContentType = request.Image.ContentType;

            await _context.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
        public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
           var Service = await _context.Services.FindAsync(id);

            if (Service is null)
                return Result.Failure(ServiceErrors.ServiceNotFound);

             _context.Remove(Service);

            await _context.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }


    }
}
