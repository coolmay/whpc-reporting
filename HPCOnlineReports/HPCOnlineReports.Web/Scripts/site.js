/* chart functions */
var chart_axis_template = {
    tooltip: {
        trigger: 'axis'
    },
    toolbox: {
        show: true,
        y: 'center',
        orient: 'vertical',
        feature: {
            mark: {
                show: true,
                title: {
                    mark: 'New markline',
                    markUndo: 'Erase markline',
                    markClear: 'Clear all marklines'
                },
            },
            magicType: {
                show: true,
                title: {
                    line: 'Line view',
                    bar: 'Bar view',
                    stack: 'Stack view',
                    tiled: 'Tiled view'
                },
                type: ['line', 'bar', 'stack', 'tiled']
            },
            dataView: {
                show: true,
                title: 'Data view',
                readOnly: false
            },
            restore: {
                show: true,
                title: 'Restore'
            },
            saveAsImage: {
                show: true,
                title: 'Save as image',
                lang: ['Right click and save']
            }
        }
    },
    calculable: true,
    title: {
        text: '',
        subtext: '',
        x: 'center'
    },
    legend: {
        x: 'center',
        y: 'bottom',
        data: []
    },
    xAxis: [
        {
            type: 'category',
            data: []
        }
    ],
    yAxis: [
        {
            type: 'value',
        },
        {
            type: 'value',
        }
    ],
    series: []
};

function drawChart(chartdivid, chartoption) {
    var chartdiv = $("#" + chartdivid);
    if (chartdiv.length <= 0 || isNullOrEmpty(chartoption)) {
        return;
    }

    // draw chart
    try {
        //if (chartdiv.height() < 400)
        //    chartdiv.height(400);
        chartdiv.show();

        var chart = echarts.init(chartdiv[0]);
        chart.setOption(chartoption);
    }
    catch (ex) {
        chartdiv.hide();
        showError(ex);
    }
}


/* site functions */
function capacity_generate_charts_clicked() {
    var div = $("#div_charts");
    if (div.length <= 0) {
        return;
    }

    var btn = $("#btn_generate_charts");
    var cluster = $("#cluster").val();
    var starttime = $("#starttime").val();
    var endtime = $("#endtime").val();

    if (isNullOrEmpty(cluster)) {
        alert("Cluster is empty.");
        return;
    }

    if (isNullOrEmpty(starttime)) {
        alert("Start time is empty.");
        return;
    }

    if (isNullOrEmpty(endtime)) {
        alert("End time is empty.");
        return;
    }

    btn.button('loading');
    AddLoadingMark("div_charts");

    var apiurl = "/Reports/GetCapacityCharts";
    var apidata = "c=" + encodeURIComponent(cluster) + "&s=" + encodeURIComponent(starttime) + "&e=" + encodeURIComponent(endtime);

    AjaxPostAsync(
        apiurl,
        apidata,
        function (data) {
            div.empty();

            if (isNullOrEmpty(data) || data.length == 0) {
                // no data
                showMessage("No data found!", "div_charts");
            }
            else {
                $.each(data, function (index, item) {
                    // create charts
                    var id = item.Id;
                    var title = item.Title;
                    var subtitle = item.Subtitle;
                    var legend = item.Legend;
                    var xaxisdata = item.XAxisData;
                    var yaxisdata = item.YAxisData;
                    var issmallsize = item.IsSmallSize;

                    if (issmallsize) {
                        div.append($("<div class='chart chart-inline' id='" + id + "'></div>"));
                    }
                    else {
                        div.append($("<div class='chart' id='" + id + "'></div>"));
                    }
                    var option = chart_axis_template;
                    option.title.text = title;
                    option.title.subtext = subtitle;
                    option.legend.data = legend;
                    option.xAxis[0].data = xaxisdata;
                    option.series = yaxisdata;

                    drawChart(id, option);
                });
            }
        },
        null,
        function () {
            btn.button('reset');
        },
        "div_charts"
    );
}

function chargeback_generate_reports_clicked() {
    var div = $("#div_owners");
    if (div.length <= 0) {
        return;
    }

    var btn = $("#btn_generate_reports");
    var cluster = $("#cluster").val();
    var reporttype = $("#report_type").val();
    var startdate = $("#startdate").val();

    if (isNullOrEmpty(cluster)) {
        alert("Cluster is empty.");
        return;
    }

    if (isNullOrEmpty(reporttype)) {
        alert("Report type is empty.");
        return;
    }

    if (isNullOrEmpty(startdate)) {
        alert("Start date is empty.");
        return;
    }

    btn.button('loading');
    AddLoadingMark("div_owners");

    var total = 0;
    var apiurl = "/Reports/GetChargeBackReports";
    var apidata = "c=" + encodeURIComponent(cluster) + "&t=" + encodeURIComponent(reporttype) + "&d=" + encodeURIComponent(startdate);

    AjaxPostAsync(
        apiurl,
        apidata,
        function (data) {
            div.empty();

            if (isNullOrEmpty(data) || data.length == 0) {
                // no data
                showMessage("No data found!", "div_owners");
            }
            else {
                $.each(data, function (index, item) {
                    // create reports
                    var owner = item.Owner;
                    var cost = item.Cost;

                    var output = $("<a id='owner_" + owner + "' href='javascript:void(0);' class='list-group-item' onclick='chargeback_ownerlist_clicked(\"" + usernameFormat(owner) + "\");'>"
                        + "<span class='badge'>" + moneyFormat(cost) + "</span>"
                        + "<strong>" + owner + "</strong>"
                        + "</a>");
                    div.append(output);

                    if (index == 0) {
                        // load first item defaultly
                        output.addClass("active");
                        chargeback_get_jobdata(owner);
                    }

                    total += cost;
                });
            }

            $("#owner_msg").html("Total: " + moneyFormat(total));
        },
        null,
        function () {
            btn.button('reset');
        },
        "div_owners"
    );
}

function chargeback_ownerlist_clicked(owner) {
    var listdiv = $("#div_owners");
    if (listdiv.length > 0) {
        var items = listdiv.children();
        for (var i = 0; i < items.length; i++) {
            var item = items[i];
            if (item.id == "owner_" + owner) {
                item.classList.add("active");
            }
            else {
                item.classList.remove("active");
            }
        }
    }

    chargeback_get_jobdata(owner);
}

function chargeback_get_jobdata(owner) {
    var div = $("#div_jobs");
    if (div.length <= 0 || isNullOrEmpty(owner)) {
        return;
    }

    var cluster = $("#cluster").val();
    var reporttype = $("#report_type").val();
    var startdate = $("#startdate").val();

    if (isNullOrEmpty(cluster)) {
        alert("Cluster is empty.");
        return;
    }

    if (isNullOrEmpty(reporttype)) {
        alert("Report type is empty.");
        return;
    }

    if (isNullOrEmpty(startdate)) {
        alert("Start date is empty.");
        return;
    }

    $("#job_msg").html("Job Report (" + reporttype + ") for " + owner + " from " + startdate + ".");
    AddLoadingMark("div_jobs");

    var apiurl = "/Reports/GetChargeBackJobData";
    var apidata = "c=" + encodeURIComponent(cluster) + "&t=" + encodeURIComponent(reporttype) + "&d=" + encodeURIComponent(startdate) + "&o=" + encodeURIComponent(owner);

    AjaxPostAsync(
        apiurl,
        apidata,
        function (data) {
            div.empty();

            if (isNullOrEmpty(data) || data.length == 0) {
                // no data
                showMessage("No data found!", "div_jobs");
            }
            else {
                $.each(data, function (index, item) {
                    // create reports
                    var jobid = item.JobId;
                    var jobname = item.JobName;
                    var cost = item.Cost;

                    var output = $("<a href='javascript:void(0);' class='list-group-item' onclick='chargeback_get_jobdetails(\"" + jobid + "\", \"" + usernameFormat(owner) + "\");'>"
                        + "<strong>" + jobname + " </strong>"
                        + "<small>(jobid: " + jobid + ")</small>"
                        + "<span class='badge'>" + moneyFormat(cost) + "</span>"
                        + "<div id='div_jobdetails_" + jobid + "' class='list-group sub-list-group'></div>"
                        + "</a>");
                    div.append(output);
                });
            }
        },
        null,
        null,
        "div_jobs"
    );
}

function chargeback_get_jobdetails(jobid, owner) {
    var div = $("#div_jobdetails_" + jobid);
    if (div.length <= 0 || isNullOrEmpty(jobid) || isNullOrEmpty(owner) || !isNullOrEmpty(div.html())) {
        return;
    }

    var cluster = $("#cluster").val();
    var reporttype = $("#report_type").val();
    var startdate = $("#startdate").val();

    if (isNullOrEmpty(cluster)) {
        alert("Cluster is empty.");
        return;
    }

    if (isNullOrEmpty(reporttype)) {
        alert("Report type is empty.");
        return;
    }

    if (isNullOrEmpty(startdate)) {
        alert("Start date is empty.");
        return;
    }

    AddLoadingMark("div_jobdetails_" + jobid);

    var apiurl = "/Reports/GetChargeBackJobDetails";
    var apidata = "c=" + encodeURIComponent(cluster) + "&t=" + encodeURIComponent(reporttype) + "&d=" + encodeURIComponent(startdate) + "&o=" + encodeURIComponent(owner) + "&j=" + encodeURIComponent(jobid);

    AjaxPostAsync(
        apiurl,
        apidata,
        function (data) {
            div.empty();

            if (isNullOrEmpty(data) || data.length == 0) {
                // no data
                showMessage("No data found!", "div_jobdetails_" + jobid);
            }
            else {
                $.each(data, function (index, item) {
                    // create reports
                    var nodename = item.NodeName;
                    var noderole = item.NodeRole;
                    var nodesize = item.NodeSize;
                    var duration = item.Duration;
                    var rate = item.Rate;
                    var cost = item.Cost;

                    var output = $("<a href='javascript:void(0);' class='list-group-item sub-list-group-item'>"
                        + "<span class='badge'>" + moneyFormat(cost) + "</span>"
                        + "<div><strong>Node Name : </strong>" + "<small>" + nodename + " </small></div>"
                        + "<div><strong>Node Role : </strong>" + "<small>" + noderole + " </small></div>"
                        + "<div><strong>Node Size : </strong>" + "<small>" + nodesize + " </small></div>"
                        + "<div><strong>Duration  : </strong>" + "<small>" + numberFormat(duration, 3) + " (hrs)</small></div>"
                        + "<div><strong>Rate      : </strong>" + "<small>" + numberFormat(rate, 3) + " ($/hr)</small></div>"
                        + "</a>");
                    div.append(output);
                });
            }
        },
        null,
        null,
        "div_jobdetails_" + jobid
    );
}

function clusterutilization_generate_charts_clicked() {
    var div = $("#div_charts");
    if (div.length <= 0) {
        return;
    }

    var btn = $("#btn_generate_charts");
    var cluster = $("#cluster").val();
    var starttime = $("#starttime").val();
    var endtime = $("#endtime").val();

    if (isNullOrEmpty(cluster)) {
        alert("Cluster is empty.");
        return;
    }

    if (isNullOrEmpty(starttime)) {
        alert("Start time is empty.");
        return;
    }

    if (isNullOrEmpty(endtime)) {
        alert("End time is empty.");
        return;
    }

    btn.button('loading');
    AddLoadingMark("div_charts");

    var apiurl = "/Reports/GetClusterUtilizationCharts";
    var apidata = "c=" + encodeURIComponent(cluster) + "&s=" + encodeURIComponent(starttime) + "&e=" + encodeURIComponent(endtime);

    AjaxPostAsync(
        apiurl,
        apidata,
        function (data) {
            div.empty();

            if (isNullOrEmpty(data) || data.length == 0) {
                // no data
                showMessage("No data found!", "div_charts");
            }
            else {
                $.each(data, function (index, item) {
                    // create charts
                    var id = item.Id;
                    var title = item.Title;
                    var subtitle = item.Subtitle;
                    var legend = item.Legend;
                    var xaxisdata = item.XAxisData;
                    var yaxisdata = item.YAxisData;
                    var issmallsize = item.IsSmallSize;

                    if (issmallsize) {
                        div.append($("<div class='chart chart-inline' id='" + id + "'></div>"));
                    }
                    else {
                        div.append($("<div class='chart' id='" + id + "'></div>"));
                    }
                    var option = chart_axis_template;
                    option.title.text = title;
                    option.title.subtext = subtitle;
                    option.legend.data = legend;
                    option.xAxis[0].data = xaxisdata;
                    option.series = yaxisdata;

                    drawChart(id, option);
                });
            }
        },
        null,
        function () {
            btn.button('reset');
        },
        "div_charts"
    );
}
