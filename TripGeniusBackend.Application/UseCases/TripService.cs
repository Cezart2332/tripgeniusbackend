using TripGeniusBackend.Application.DTOs.Trip;
using TripGeniusBackend.Application.Interfaces;
using TripGeniusBackend.Application.Interfaces.Queries;
using TripGeniusBackend.Domain.Entities;
using TripGeniusBackend.Domain.Enums;

namespace TripGeniusBackend.Application.UseCases;

public class TripService : ITripService
{
    private readonly ITripRepository _tripRepository;
    private readonly ITripQueryService _tripQueryService;
    private readonly IUserRepository _userRepository;
    private readonly IUserQueryService _userQueryService;
    private readonly IJwtService _jwtService;
    private readonly IFileUploader _fileUploader;

    public TripService(ITripRepository tripRepository,ITripQueryService tripQueryService, IUserRepository userRepository,IUserQueryService userQueryService, IJwtService jwtService, IFileUploader fileUploader)
    {
        _tripRepository = tripRepository;
        _tripQueryService = tripQueryService;
        _userRepository = userRepository;
        _userQueryService = userQueryService;
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


    public async Task<List<TripResponse>> GetTripsForUser(TripsRequest tripsRequest)
    {
        var user = await _userRepository.GetUserById(_jwtService.GetUserId());
        var trips = await _tripQueryService.GetTrips(user.Id);

        var filtered = trips.Where(t =>
            t.Status.Equals(Status.Upcoming.ToString()) && t.Price <= tripsRequest.Budget && t.MaxParticipants > t.Members.Count &&
            t.MaxParticipants <= user.Preferences.MaxGroupSize && t.Title.ToLower().Contains(tripsRequest.Search.ToLower()));
        filtered = tripsRequest.Preferences
            ? filtered.Where(t => user.Preferences.Tags.Any(tag => t.Tags.Contains(tag))) 
                : tripsRequest.Tag.Equals("all") ? filtered : filtered.Where(t => t.Tags.Contains(tripsRequest.Tag));
        return filtered.ToList();
    }
    
    public async Task<TripResponse> GetTrip(int tripId)
    {
        var trip = await _tripQueryService.GetTripById(tripId, _jwtService.GetUserId());
        if(trip == null) throw new ArgumentException("Trip not found");
        return trip;
    }

    public async Task<List<TripResponse>> GetUserTrips()
    {
        var user = await _userRepository.GetUserById(_jwtService.GetUserId());
        var trips = await _tripQueryService.GetTrips(user.Id);
        var filtered = trips.Where(t => t.isUserMember).ToList();
        
        return filtered.ToList();
    }
    
    

}