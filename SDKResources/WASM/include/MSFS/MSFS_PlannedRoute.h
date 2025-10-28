#ifndef _MSFS_PLANNEDROUTE_H
#define _MSFS_PLANNEDROUTE_H

#include "MSFS_FlightPlan.h"

// For backwards compatibility
typedef FsIcao FsRouteIcao;

#ifdef __cplusplus
extern "C" {
#endif

  typedef enum
  {
    FsFlightAltitudeType_None,
    FsFlightAltitudeType_Feet,
    FsFlightAltitudeType_FlightLevel
  } FsFlightAltitudeType;

  typedef enum
  {
    FsEnrouteLegType_Normal,
    FsEnrouteLegType_LatLon,
    FsEnrouteLegType_PointBearingDistance
  } FsEnrouteLegType;

  #pragma pack(push, 1)

  struct FsVisualPattern
  {
    int pattern;
    bool isLeftTraffic;
    float distance;
    float altitude;
  };
  typedef struct FsVisualPattern;

  struct FsFlightAltitude
  {
    FsFlightAltitudeType type;
    int altitude;
  };
  typedef struct FsFlightAltitude FsFlightAltitude;

  struct FsEnrouteLeg
  {
    FsEnrouteLegType type;
    FsRouteIcao fixIcao;
    char via[9];
	char* name;
	FsFlightAltitude altitude;
    double lat;
    double lon;
    FsRouteIcao pbdReferenceIcao;
    double bearing;
    double distance;
  };
  typedef struct FsEnrouteLeg FsEnrouteLeg;

  struct FsPlannedRoute
  {
    FsRouteIcao departureAirport;
    FsRunwayIdentifier departureRunway;
    char departure[9];
    char departureTransition[9];
    FsVisualPattern departureVisualPattern;
    FsRouteIcao destinationAirport;
    FsRunwayIdentifier destinationRunway;
    char arrival[9];
    char arrivalTransition[9];
    FsApproachIdentifier approach;
    char approachTransition[9];
    FsVisualPattern approachVisualPattern;
    FsFlightAltitude cruiseAltitude;
    bool isVfr;
    int numEnrouteLegs;
    FsEnrouteLeg* enrouteLegs;
  };
  typedef struct FsPlannedRoute FsPlannedRoute;

  #pragma pack(pop)

  typedef long FsRouteRequestId;
  typedef void (*fsPlannedRouteBroadcastCallback)(const FsPlannedRoute* route, void* ctx);
  typedef void (*fsPlannedRouteRequestCallback)(FsRouteRequestId id, void* ctx);

  extern FsPlannedRoute* fsPlannedRouteGetEfbRoute();
  extern bool fsPlannedRouteRegisterForBroadcast(fsPlannedRouteBroadcastCallback callback, void* ctx);
  extern bool fsPlannedRouteUnregisterForBroadcast(fsPlannedRouteBroadcastCallback callback);
  extern bool fsPlannedRouteRegisterForRequest(fsPlannedRouteRequestCallback callback, void* ctx);
  extern bool fsPlannedRouteUnregisterForRequest(fsPlannedRouteRequestCallback callback);
  extern bool fsPlannedRouteRespondToRequest(FsRouteRequestId id, FsPlannedRoute* route);

#ifdef __cplusplus
}
#endif

#endif