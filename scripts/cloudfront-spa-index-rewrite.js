// CloudFront Function (viewer-request): map /admin/ -> /admin/index.html for S3 REST origins.
function handler(event) {
  var request = event.request;
  var uri = request.uri;

  if (uri.startsWith("/api/")) {
    return request;
  }

  if (uri.endsWith("/")) {
    request.uri = uri + "index.html";
  } else if (!uri.includes(".")) {
    request.uri = uri + "/index.html";
  }

  return request;
}
