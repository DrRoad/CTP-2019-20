% Load luma data from LDR/HDR images matching ID
STREETVIEW_ID = 'xdU_R-qfflPfs8x-tTKM8g';
[hdrlum, ldrlum] = LoadFromID(STREETVIEW_ID);

% Create a histogram from the luma data
hdrhist = hist(reshape(hdrlum, [64 * 128, 1]), 100);
ldrhist = hist(reshape(ldrlum, [size(ldrlum, 1) * size(ldrlum, 2), 1]), 100);

% Bring our values down to a normalised range
hdrhist = hdrhist ./ (64 * 128);
ldrhist = ldrhist ./ (size(ldrlum, 1) * size(ldrlum, 2));

% Plot out the values
clf;
plot(ldrhist, 'DisplayName', 'LDR Luma');
hold on;
plot(hdrhist, 'DisplayName', 'HDR Luma');
%legend;

% Try and match both distributions
hdrhistmod = zeros(1, 100);
hdrhistdiff = zeros(1, 100);
leftover = 0;
for x = 1:100
    thisLDRValue = ldrhist(1, x);
    thisHDRValue = hdrhist(1, x);
    
    leftover = leftover + thisHDRValue;
    hdrhistmod(1, x) = thisLDRValue;
    leftover = leftover - thisLDRValue;
    hdrhistdiff(1, x) = leftover;
    
    if leftover <= 0
        %error("Out of entries at iteration " + c);
        break;
    end
end

% Re-plot
hold on;
plot(hdrhistmod, 'DisplayName', 'HDR Luma Adjusted');
hold on;
plot(hdrhistdiff, 'DisplayName', 'HDR Luma Diff');
legend;