// eslint-disable-next-line @typescript-eslint/no-explicit-any
let timer: any = null;
const edgeSize = 120;

// Original auto scroll code from the link below.
// the code has been modified to base the viewport rect on a desired div rather than the window.
// https://www.bennadel.com/blog/3460-automatically-scroll-the-window-when-the-user-approaches-the-viewport-edge-in-javascript.htm

export const mouseMoveHandler = (e: MouseEvent) => {
  const scrollable = document.querySelector('.board-groups');

  if (!scrollable) return;

  // NOTE: Much of the information here, with regard to document dimensions,
  // viewport dimensions, and window scrolling is derived from JavaScript.info.
  // I am consuming it here primarily as NOTE TO SELF.
  // --
  // Read More: https://javascript.info/size-and-scroll-window
  // --
  // CAUTION: The viewport and document dimensions can all be CACHED and then
  // recalculated on window-resize events (for the most part). I am keeping it
  // all here in the mousemove event handler to remove as many of the moving
  // parts as possible and keep the demo as simple as possible.

  // Get the viewport-relative coordinates of the mousemove event.
  const viewportX = e.clientX;
  const viewportY = e.clientY;

  // Get the viewport dimensions.
  const viewportRects = scrollable.getBoundingClientRect();

  const viewportLeft = viewportRects.left;
  const viewportTop = viewportRects.top;
  const viewportWidth = scrollable.clientWidth;
  const viewportHeight = scrollable.clientHeight;

  // Next, we need to determine if the mouse is within the "edge" of the
  // viewport, which may require scrolling the window. To do this, we need to
  // calculate the boundaries of the edge in the viewport (these coordinates
  // are relative to the viewport grid system).
  const edgeTop = edgeSize + viewportTop;
  const edgeLeft = edgeSize + viewportLeft;
  const edgeBottom = viewportRects.bottom - edgeSize;
  const edgeRight = viewportRects.right - edgeSize;

  const isInLeftEdge = viewportX < edgeLeft;
  const isInRightEdge = viewportX > edgeRight;
  const isInTopEdge = viewportY < edgeTop;
  const isInBottomEdge = viewportY > edgeBottom;

  // If the mouse is not in the viewport edge, there's no need to calculate
  // anything else.
  if (!(isInLeftEdge || isInRightEdge || isInTopEdge || isInBottomEdge)) {
    if (timer) {
      clearTimeout(timer);
    }
    return;
  }

  // If we made it this far, the user's mouse is located within the edge of the
  // viewport. As such, we need to check to see if scrolling needs to be done.

  // Get the document dimensions.
  // --
  // NOTE: The various property reads here are for cross-browser compatibility
  // as outlined in the JavaScript.info site (link provided above).
  const scrollableWidth = Math.max(
    scrollable.scrollWidth,
    scrollable.clientWidth
  );
  const scrollableHeight = Math.max(
    scrollable.scrollHeight,
    scrollable.clientHeight
  );

  // Calculate the maximum scroll offset in each direction. Since you can only
  // scroll the overflow portion of the document, the maximum represents the
  // length of the document that is NOT in the viewport.
  const maxScrollX = scrollableWidth - viewportWidth;
  const maxScrollY = scrollableHeight - viewportHeight;

  // Adjust the window scroll based on the user's mouse position. Returns True
  // or False depending on whether or not the window scroll was changed.
  const adjustWindowScroll = () => {
    // Get the current scroll position of the document.
    const currentScrollX = scrollable.scrollLeft;
    const currentScrollY = scrollable.scrollTop;

    // Determine if the window can be scrolled in any particular direction.
    const canScrollUp = currentScrollY > 0;
    const canScrollDown = currentScrollY < maxScrollY;
    const canScrollLeft = currentScrollX > 0;
    const canScrollRight = currentScrollX < maxScrollX;

    // Since we can potentially scroll in two directions at the same time,
    // let's keep track of the next scroll, starting with the current scroll.
    // Each of these values can then be adjusted independently in the logic
    // below.
    let nextScrollX = currentScrollX;
    let nextScrollY = currentScrollY;

    // As we examine the mouse position within the edge, we want to make the
    // incremental scroll changes more "intense" the closer that the user
    // gets the viewport edge. As such, we'll calculate the percentage that
    // the user has made it "through the edge" when calculating the delta.
    // Then, that use that percentage to back-off from the "max" step value.
    const maxStep = 20;

    // Should we scroll left?
    if (isInLeftEdge && canScrollLeft) {
      const intensity = (edgeLeft - viewportX) / edgeSize;

      nextScrollX = nextScrollX - maxStep * intensity;

      // Should we scroll right?
    } else if (isInRightEdge && canScrollRight) {
      const intensity = (viewportX - edgeRight) / edgeSize;

      nextScrollX = nextScrollX + maxStep * intensity;
    }

    // Should we scroll up?
    if (isInTopEdge && canScrollUp) {
      const intensity = (edgeTop - viewportY) / edgeSize;

      nextScrollY = nextScrollY - maxStep * intensity;

      // Should we scroll down?
    } else if (isInBottomEdge && canScrollDown) {
      const intensity = (viewportY - edgeBottom) / edgeSize;

      nextScrollY = nextScrollY + maxStep * intensity;
    }

    // Sanitize invalid maximums. An invalid scroll offset won't break the
    // subsequent .scrollTo() call; however, it will make it harder to
    // determine if the .scrollTo() method should have been called in the
    // first place.
    nextScrollX = Math.max(0, Math.min(maxScrollX, nextScrollX));
    nextScrollY = Math.max(0, Math.min(maxScrollY, nextScrollY));

    if (nextScrollX !== currentScrollX || nextScrollY !== currentScrollY) {
      scrollable.scrollTo({
        left: nextScrollX,
        top: nextScrollY,
        behavior: 'auto',
      });
      return true;
    } else {
      return false;
    }
  };

  // As we examine the mousemove event, we want to adjust the window scroll in
  // immediate response to the event; but, we also want to continue adjusting
  // the window scroll if the user rests their mouse in the edge boundary. To
  // do this, we'll invoke the adjustment logic immediately. Then, we'll setup
  // a timer that continues to invoke the adjustment logic while the window can
  // still be scrolled in a particular direction.
  // --
  // NOTE: There are probably better ways to handle the ongoing animation
  // check. But, the point of this demo is really about the math logic, not so
  // much about the interval logic.
  const checkForWindowScroll = () => {
    if (timer) {
      clearTimeout(timer);
    }

    adjustWindowScroll();
    timer = setTimeout(checkForWindowScroll, 20);
  };

  checkForWindowScroll();
};
