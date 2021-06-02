# Requirements

## V1 Requirements
The goal of DrawTogether.NET is to allow users to collaboratively produce drawings together using an MS Paint-style interface. The tools that are available to do this should be simple and exportable to persistent image formats if so desired.

DrawTogether.NET should be usable over any relatively modern web browser (Chrome, Brave) and should prioritize snapiness and responiveness over graphical complexity and data consistency.

* All drawing sessions can be anonymous;
* Drawing output won't be persisted for longer than N minutes after the last user disconnects (TTL - time to live);
* Source of truth will be the server;
* Drawings will be schemaless - so multiple writers can all work in concert together;
* All user activity needs to be synced in soft-realtime (server-push);
* Need to support large number of concurrent users AND shapes with many brush strokes;
* Entire application should be capable of running in Docker; 
* Server should be able to recover documents that were produced on one node, recovered on another; and
* Must have redundancy in all services.

### Functionality
Overall app functionality:

1. Create a new drawing with a button click, launches a blank canvas with a unique url;
2. Anyone can join ongoing drawing session by visting that Url;
3. A set of actively used drawings and their concurrent user count should be displayed on homepage;
4. Anyone can export the current state of a drawing to PNG format;
5. Cursors for all active users will be shown live on-screen even if they aren't drawing anything;
6. All anonymous users will be assigned a random but persistent name; and
7. Any user can join a drawing at any point in its progress.

Drawing functionality - we need to support:

1. Pencils with varying colors and thickness;
2. Eraser; and
3. Draw shapes with or without fills.