(function ($) {
    $(document).ready(function () {
        refreshLists();
        $("#merge").click(onMergeClicked);
        $("#base").change(onChange);
    });

    function onChange() {
        var $baseSelected = $("#base").find(":selected");
        var baseData = $baseSelected.data("rr");
        $("#checkboxes input").each(function () {
            var $chb = $(this);
            var chbData = $chb.data("rr");

            if (chbData.Submitter == baseData.Submitter && chbData.Id > baseData.Id) {
                $chb.parent().show();
            }
            else {
                $chb.parent().hide();
                $chb.prop("checked", false);
            }
        });
    }

    function refreshLists() {
        $.get("/getReviewRequests")
            .done(function (data) {
                loadReviewLists(data);
                onChange();
            })
            .fail(function (jqXHR, textStatus, errorThrown) {
                alert("There was an error while loading ReviewBoard URL: " + errorThrown);
            });
    }

    function loadReviewLists(data) {
        var $base = $("#base");
        var $panel = $("#checkboxes");
        $base.empty();
        $.each(data, function () {
            var $option = $("<option></option>").attr("value", this.Id).text(this.Id + ": " + this.Submitter + " - " + this.Summary);
            $base.prepend($option.clone().data("rr", this));

            var $checkbox = $("<input type='checkbox'>")
                .attr("id", "rr" + this.Id)
                .attr("name", "review_requests_multiple")
                .attr("value", this.Id)
                .data("rr", this);
            var $label = $("<label>")
                .attr("for", "rr" + this.Id)
                .text(this.Id + ": " + this.Submitter + " - " + this.Summary);
            $panel.prepend($("<div>").append($checkbox).append($label));
        });

    }

    function onMergeClicked() {
        var $base = $("#base");
        var baseValue = $base.val();
        var $checkboxes = $("#checkboxes input:checked");

        var ids = [];
        $checkboxes.each(function () {
            var $chb = $(this);
            var chbData = $chb.data("rr");
            ids.push(chbData.Id);
        });

        $("#merge").prop("enabled", false);
        $.ajax("/mergeReviews",
            {
                method: "POST",
                data: "{ \"BaseId\": \"" + baseValue + "\", \"SubsequentIds\": [\"" + ids.join("\",\"") + "\"] }",
                success: function (data, text, jqXHR) {
                    $("#merge").prop("enabled", true);
                    loadReviewLists(data);
                    onChange();
                },
                contentType: "application/json"
            });
    }

})(jQuery);