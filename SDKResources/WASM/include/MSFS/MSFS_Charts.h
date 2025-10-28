//-----------------------------------------------------------------------------
//
// Copyright (c) Microsoft Corporation. All Rights Reserved.
//
//-----------------------------------------------------------------------------

#ifndef _MSFS_CHARTS_H
#define _MSFS_CHARTS_H

#include "MSFS_Core.h"
#include "MSFS_FlightPlan.h"

#ifdef __cplusplus
extern "C" {
#endif

// Forward declarations
#pragma pack(push, 4)

/**
 * @brief A charts API error code.
 */
typedef enum FsChartError : unsigned char
{
	/**
	 * @brief No error.
	 */
	FsChartError_None = 0,
	/**
	 * @brief The requested chart GUID was not found.
	 */
	FsChartError_NotFound,
	/**
	 * @brief The requested chart provider was not found.
	 */
	FsChartError_UnknownProvider,
	/**
	 * @brief A generic network error. This request can be retried.
	 */
	FsChartError_NetworkError,
	/**
	 * @brief A generic internal error. This request should not be retried.
	 */
	FsChartError_InternalError,
} FsChartError;

struct FsChartIndexCategory;

/**
 * @brief A chart index for an airport.
 */
typedef struct FsChartIndex
{
	/**
	 * @brief The ICAO of the airport this chart index is for.
	 */
	FsIcao airportIcao;

	/**
	 * @brief An array of chart sets, organized in categories, in this index.
	 */
	FsChartIndexCategory* chartCategories;

	/**
	 * @brief The number of chart categories in this index.
	 */
	int numChartCategories;
} FsChartIndex;

struct FsChartMetadata;

/**
 * @brief A set of charts belonging to one category.
 */
typedef struct FsChartIndexCategory
{
	/**
	 * @brief The human-readable name of the category.
	 */
	const char* name;

	/**
	 * @brief The charts present in the category.
	 */
	FsChartMetadata* charts;

	/**
	 * @brief The number of charts present in this category.
	 */
	int numCharts;
} FsChartIndexCategory;

/**
 * @brief A chart procedure identifier type
 */
typedef enum FsChartProcedureIdentifierType
{
	FsChartProcedureIdentifierType_Sid,
	FsChartProcedureIdentifierType_Star,
	FsChartProcedureIdentifierType_Approach,
} FsChartProcedureIdentifierType;

/**
 * @brief A procedure identifier, linking a chart to a procedure at its airport.
 */
typedef struct FsChartProcedureIdentifier
{
	/**
	 * @brief The type of procedure being identified.
	 */
	FsChartProcedureIdentifierType type;

	/**
	 * @brief The procedure's 7-letter identifier, empty if an approach.
	 */
	char ident[8];

	/**
	 * @brief The procedure's approach identifier, if it is an approach. Only valid if `hasApproachIdentifier` is true.
	 */
	FsApproachIdentifier approachIdentifier;

	/**
	 * @brief Whether the `approachIdentifier` is present and valid.
	 */
	bool hasApproachIdentifier;

	/**
	 * @brief The identifiers of the runways this procedure if applicable for.
	 */
	FsRunwayIdentifier* runways;

	/**
	 * @brief The number of runways this procedure is applicable for.
	 */
	int numRunways;

	/**
	 * @brief The runway transition, if applicable, that this identifier is pointing to.
	 * @deprecated This field is deprecated and will always be empty.
	*/
	char runwayTransition[8];

	/**
	 * @brief The enroute transition, if applicable, that this identifier is pointing to. Only valid if `hasEnrouteTransition` is true.
	 */
	char enrouteTransition[8];

	/**
	 * @brief Whether the `enrouteTransition` is present and valid.
	 */
	bool hasEnrouteTransition;
} FsChartProcedureIdentifier;

/**
 * @brief A chart relationship type.
 */
typedef enum FsChartRelationshipType
{
	FsChartRelationshipType_ProcedureTextualToGraphical,
	FsChartRelationshipType_ProcedureGraphicalToTextual,
} FsChartRelationshipType;

/**
 * @brief A relationship between two charts.
 */
typedef struct FsChartRelationship
{
	/**
	 * @brief The GUID of the chart the relationship is from.
	 */
	const char* fromChartGuid;

	/**
	 * @brief The GUID of the chart the relationship is to.
	 */
	const char* toChartGuid;

	/**
	 * @brief The specific page number (0-indexed) into the "to" chart at which the relationship applies, if applicable.
	 *
	 * If the value is -1, the relationship applies to all pages of the chart.
	 */
	int toChartPage;

	/**
	 * @brief The type of relationship this chart has with the chart it is related to.
	 */
	FsChartRelationshipType type;

	/**
	 * @brief The procedure for which the relationship is applicable, if any. Only valid if `hasProcedure` is true.
	 */
	FsChartProcedureIdentifier procedure;

	/**
	 * @brief Whether the `procedure` is present and valid.
	 */
	bool hasProcedure;
} FsChartRelationship;

/**
 * @brief Metadata regarding an airport chart, part of a chart index.
 */
typedef struct FsChartMetadata
{
	/**
	 * @brief The chart's GUID.
	 */
	const char* guid;

	/**
	 * @brief The chart's human readable name.
	 */
	const char* name;

	/**
	 * @brief The provider of this chart.
	 */
	const char* provider;

	/**
	 * @brief The ICAO of this chart's airport.
	 */
	FsIcao airportIcao;

	/**
	 * @brief An array of runways this chart is related to.
	 */
	FsRunwayIdentifier* runways;

	/**
	 * @brief The number of runways this chart is related to.
	 */
	int numRunways;

	/**
	 * @brief An array of procedures this chart is related to.
	 */
	FsChartProcedureIdentifier* procedures;

	/**
	 * @brief The number of procedures this chart is related to.
	 */
	int numProcedures;

	/**
	 * @brief The aircraft types this chart is specifically applicable to, empty if applicable to all.
	 */
	const char** aircraftTypes;

	/**
	 * @brief The number of aircraft types this chart is applicable to.
	 */
	int numAircraftTypes;

	/**
	 * @brief A list of relationships from this chart to other charts.
	 */
	FsChartRelationship* relationships;

	/**
	 * @brief The number of relationships this chart has with other charts.
	 */
	int numRelationships;

	/**
	 * @brief The chart's type. What this means semantically is dependent on the specific provider which returned this chart.
	 */
	const char* type;

	/**
	 * @brief The UNIX timestamp beginning from which the chart is valid, if applicable. If not applicable, this value will be 0.
	 */
	unsigned long long validFrom;

	/**
	 * @brief The UNIX timestamp until which the chart is valid, if applicable. If not applicable, this value will be 0.
	 */
	unsigned long long validUntil;

	/**
	 * @brief Whether any of the chart's pages are geo-referenced.
	 */
	bool geoReferenced;
} FsChartMetadata;

struct FsChartPage;

/**
 * @brief A collection of chart pages.
 */
typedef struct FsChartPages
{
	/**
	 * @brief The array of chart pages.
	 */
	FsChartPage* pages;

	/**
	 * @brief The number of chart pages.
	 */
	int numPages;
} FsChartPages;

/**
 * @brief A rectangle on a chart, either in pixels coordinates or in long/lat coordinates.
 */
typedef struct FsChartRectangle
{
	/**
	 * @brief The upper left coordinates of the rectangle, either X, Y or Lon, Lat.
	 */
	double upperLeft[2];

	/**
	 * @brief The lower right coordinates of the rectangle, either X, Y or Lon, Lat.
	 */
	double lowerRight[2];

	/**
	 * @brief The orientation, in true degrees, of the rectangle.
	 */
	double orientation;
} FsChartRectangle;

/**
 * @brief A Lambert Conformal Conic projection for a chart area.
 */
typedef struct FsChartGeoReferenceLambertConformalConicProjection
{
	/**
	 * @brief The first standard parallel of the projection.
	 */
	double standardParallel1;

	/**
	 * @brief The second standard parallel of the projection.
	 */
	double standardParallel2;

	/**
	 * @brief The central meridian of the projection.
	 */
	double centralMeridian;
} FsChartGeoReferenceLambertConformalConicProjection;

/**
 * @brief An area on a chart, possible geo-referenced.
 */
typedef struct FsChartArea
{
	/**
	 * @brief The layer name.
	 */
	const char* layer;

	/**
	 * @brief Whether the area is geo-referenced. If it is, WorldRectangle and Projection will have values.
	 */
	bool geoReferenced;

	/**
	 * @brief A rectangle giving pixel coordinates of the area on the chart.
	 */
	FsChartRectangle chartRectangle;

	/**
	 * @brief A rectangle giving lon/lat coordinates of the area on the chart. Only valid if the chart is geo-referenced (`geoReferenced` is set to true).
	 */
	FsChartRectangle worldRectangle;

	/**
	 * @brief The projection used by the area. Only valid if the chart is geo-referenced (`geoReferenced` is set to true).
	 */
	FsChartGeoReferenceLambertConformalConicProjection projection;
} FsChartArea;

typedef struct FsChartPageUrl
{
	/**
	 * @brief An identifier for the URL, ideally containing useful information about the file type.
	 */
	const char* name;

	/**
	 * @brief The actual URL to the chart image file. This URL is only used as an identifier and can not be used to fetch the image.
	 */
	const char* url;
} FsChartPageUrl;

/**
 * @brief An individual page of a chart.
 */
typedef struct FsChartPage
{
	/**
	 * @brief Width of the chart in pixels.
	 */
	unsigned int width;

	/**
	 * @brief Height of the chart in pixels.
	 */
	unsigned int height;

	/**
	 * @brief Whether any of the areas of the charts are geo-referenced.
	 */
	bool geoReferenced;

	/**
	 * @brief An array of areas on this chart.
	 */
	FsChartArea* areas;

	/**
	 * @brief The number of areas on this chart.
	 */
	int numAreas;

	/**
	 * @brief An array of URLs for images of this page.
	 */
	FsChartPageUrl* urls;

	/**
	 * @brief The number of URLs for images of this page.
	 */
	int numUrls;
} FsChartPage;

#pragma pack(pop)

typedef void (*FsChartsIndexCallback)(FsChartError error, FsChartIndex* index, void* userData);
typedef void (*FsChartsPagesCallback)(FsChartError error, FsChartPages* index, void* userData);
typedef void (*FsChartsPageImageCallback)(FsChartError error, int pageImageId, void* userData);

extern bool fsChartsGetIndex(FsIcao airportIcao, const char* provider, FsChartsIndexCallback callback, void* userData);
extern bool fsChartsGetPages(const char* chartGuid, FsChartsPagesCallback callback, void* userData);
extern bool fsChartsGetPageImage(FsContext ctx, const char* url, FsChartsPageImageCallback callback, void* wasmUserData);
extern void fsChartsFreeChartIndex(FsChartIndex* index);
extern void fsChartsFreeChartPages(FsChartPages* pages);

#ifdef __cplusplus
}
#endif

#endif  // _MSFS_CHARTS_H
