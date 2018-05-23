module Jekyll
    class SeriesNavigation < Generator
      def generate(site)
        groups = {}
        site.posts.docs
            .select { |post| post.data["series"] }
            .group_by { |post| post.data["series"] }
            .each do |series,posts|
                if posts[0].data["seriesOrder"]
                    groups[series] = posts.sort_by { |post| post.data["seriesOrder"] }
                else
                    groups[series] = posts
                end
                groups[series].each_index do |i|
                    if i != 0
                        groups[series][i].data["seriesPrevious"] = groups[series][i - 1]
                    end
                    if i != groups[series].length - 1
                        groups[series][i].data["seriesNext"] = groups[series][i + 1]
                    end
                end
            end
        site.data["series"] = groups
      end
    end
  end