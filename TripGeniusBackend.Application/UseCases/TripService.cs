using TripGeniusBackend.Application.DTOs.Trip;
using TripGeniusBackend.Application.Interfaces;
using TripGeniusBackend.Domain.Entities;
using TripGeniusBackend.Domain.Enums;

namespace TripGeniusBackend.Application.UseCases;

public class TripService : ITripService
{
    private readonly ITripRepository _tripRepository;
    private readonly IUserRepository _userRepository;
    private readonly IJwtService _jwtService;
    private readonly IFileUploader _fileUploader;

    public TripService(ITripRepository tripRepository, IUserRepository userRepository, IJwtService jwtService, IFileUploader fileUploader)
    {
        _tripRepository = tripRepository;
        _userRepository = userRepository;
        _jwtService = jwtService;
        _fileUploader = fileUploader;
    }

    public async Task CreateTrip(TripRequest tripRequest)
    {
        if(tripRequest == null) throw new ArgumentNullException("Trip request is null");
        int userId = _jwtService.GetUserId();
        var trip = Trip.Create(tripRequest.Title, tripRequest.Description, tripRequest.StartingDate,
            tripRequest.EndingDate, tripRequest.Tags, tripRequest.MaxParticipants, tripRequest.Price, userId);
        foreach (var timeline in tripRequest.Timelines)
        {
            trip.AddTimeline(timeline.Day, timeline.StartingPoint, timeline.FromCoords, timeline.EndPoint, timeline.ToCoords, timeline.Note);
        }

        await _tripRepository.CreateTrip(trip);
        await _tripRepository.SaveChanges();
        if (tripRequest.ImageStream != null)
        {
            string url = await _fileUploader.UploadFile(tripRequest.ImageStream,Path.GetExtension(tripRequest.ImageFileName),"trip", trip.Id);
            trip.SetImageUrl(url);
        }
        await _tripRepository.SaveChanges();
    }
    public Task<TripResponse> GetTrip(int tripId)
    {
        throw new NotImplementedException();
    }

    public Task<List<TripResponse>> GetTrips()
    {
        throw new NotImplementedException();
    }
}